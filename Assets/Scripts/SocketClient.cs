using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using UnityEngine.UI;
using System.Text;
using LitJson;
using UnityEngine.SceneManagement;
using System.Net.WebSockets;
using System.Threading;

public class SocketClient : MonoBehaviour
{
    public HexGrid grid;
    public GameUI gameUI;
    public Transform MsgCanvas;
    public Transform MsgBoxPrefab;
    GameObject MsgMask;
    Text MsgShowText;
    float textTime = 0f;
    int msgState = 0;
    string[] msgtext = new string[] {
        "connecting to server.",
        "connecting to server..",
        "connecting to server...",
        "sending message to server.",
        "sending message to server..",
        "sending message to server...",
        "waiting for response.",
        "waiting for response..",
        "waiting for response..."
    };

    public string token; // 口令 如：127.0.0.1:12345/1/player/0
    ClientWebSocket webSocket = null; // 主体
    //—————————————————————新变量—————————————————————
    CancellationToken ct;
    //—————————————————————新变量—————————————————————

    /*public float connectInterval = 1f; // 链接间隔时间
    public float connectTime = 0f; // 链接所用时间
    public int connectCount = 0; // 链接次数
    public bool isConnecting = false; // 是否正在链接*/

    public class _Start
    {
        public string token { get; set; }
        public string request { get; set; }
    }

    public class _Send
    {
        public string token { get; set; }
        public string request { get; set; }
        public string content { get; set; }
    }

    public class _Receive
    {
        public string request { get; set; }
        public string content { get; set; }
    }

    void Start()
    {
        msgboxStack = new Stack<GameObject>();
        MsgMask = MsgCanvas.GetChild(0).gameObject;
        MsgShowText = MsgMask.transform.GetChild(0).GetComponent<Text>();
        if (gameUI.gameMode == 1) {
            // 处理token
            token = gameUI.onlineToken;
            string decodeToken = DecodeBase64("utf-8", token);
            if (decodeToken == token) decodeSuccess = false;
            else decodeSuccess = true;
            token = decodeToken;
            Debug.Log(token);
            ConnectToServer();
        }
        beginShowPoint = 20;
    }

    bool decodeSuccess = false;
    public async void ConnectToServer() // 它是一个代替前头两个函数功能的函数
    {
        msgState = 0;
        Debug.Log("in ConnectToServer: msgState = " + msgState.ToString());
        try {
            webSocket = new ClientWebSocket();
            ct = new CancellationToken();

            Uri url;
            if (decodeSuccess) {
                url = new Uri("wss://" + token);
            }
            else {
                url = new Uri("ws://" + token);
            }
            Debug.Log(url);

            await webSocket.ConnectAsync(url, ct);

            msgState = 1;
            Debug.Log("msgState = " + msgState.ToString());
            _Start _start = new _Start();
            _start.token = EncodeBase64("utf-8", token);
            _start.request = "connect";
            string JsonStr = JsonMapper.ToJson(_start);
            Debug.Log("发送的消息时：" + JsonStr);
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonStr)),
                WebSocketMessageType.Binary, true, ct);
        }
        catch (Exception e) {
            popMsgBox("Unable to connect to the remote server!", 0);
            Debug.Log(e);
            return;
        }
        msgState = 2;
        Debug.Log("msgState = " + msgState.ToString());
        ReceiveFormServer();
    }

    string last_msg = "";
    public async void SendMessageToServer(string msg) // 它是一个代替前头两个函数功能的函数
    {
        Debug.Log("发送的消息时：" + msg);
        last_msg = msg;
        await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)),
            WebSocketMessageType.Binary, true, ct);
    }

    int player_num = 0;
    bool hasGameOver = false;
    bool hasDisConnect = false;
    public async void ReceiveFormServer()
    {
        string JsonStr = "";
        try {
            var buffer = new byte[4096];
            await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), new CancellationToken());
            JsonStr = System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            JsonStr = JsonStr.TrimEnd('\0');
            Debug.Log("接收到的消息是：" + JsonStr);

            // _Receive receive = JsonMapper.ToObject<_Receive>(JsonStr);
            JsonData data = JsonMapper.ToObject(JsonStr);
            if ((string)data["request"] == "time") {
                if (!showTime && gameUI.myCamp == grid.activeCamp) { // 若已经显示出来了就不校准了
                    operateTime = (double)data["content"];
                    if (operateTime <= (beginShowPoint + 5) * 1000) {
                        beginCount = true;
                    }
                }
            }
            else if ((string)data["request"] == "action") {
                string content = (string)data["content"];
                content = content.TrimStart('[');
                content = content.TrimEnd(']');
                string[] contentText = content.Split(',');
                int totalLen = contentText.Length;
                if (totalLen % 7 != 0) {
                    popMsgBox("command format error: totalLen = " + totalLen, 0);
                    return;
                }

                // 读取命令
                int index = 0;
                while (index < totalLen) {
                    int round = int.Parse(contentText[index]);
                    CommandType command = (CommandType)int.Parse(contentText[index + 1]);
                    int[] arg = new int[GameUI.argNum];
                    for (int i = 0; i < GameUI.argNum; i++) {
                        arg[i] = int.Parse(contentText[index + i + 2]);
                    }

                    Command C = new Command(round, command, arg);
                    grid.commands.Add(new Command(round, command, arg));

                    // 处理命令类型
                    if (C.type == CommandType.GAMESTARTED) {
                        msgState = 3;

                        // 确保gameUI.setupMenu是activeSelf的
                        gameUI.InitiateEnvFromCommands();
                        gameUI.MsgCanvasMask.SetActive(false);
                        gameUI.setupMenu.gameObject.SetActive(true);
                        if (!gameUI.setupMenu.gameObject.activeSelf) {
                            popMsgBox("setupMenu is not ready! ", 0);
                            return;
                        }
                        gameUI.setupMenu.FillContent();
                    }
                    else if (C.type == CommandType.SETUPDECK) {
                        player_num += 1;
                        if (player_num == 2) { // 游戏正式开始了
                            gameUI.InitiateDeckFromCommands();
                            gameUI.InitiateVariablesFromDeck();
                            gameUI.InitiatePanel();
                            gameUI.UpdateAllPanel();
                        }
                    }
                    else if (C != null) {
                        if (player_num < 2) {
                            popMsgBox("the other player is not ready! ", 0);
                            return;
                        }
                        else if (C.type == CommandType.ROUNDOVER) {
                            // 更新operateTime
                            if (showTime) {
                                StartCoroutine(gameUI.popTimePanel(false));
                            }
                            showTime = false;
                            beginCount = false;
                        }
                        else if (C.type == CommandType.GAMEOVER) {
                            if (C.arg[0] == gameUI.myCamp) {
                                gameoverMsg = "You win!";
                            }
                            else if (C.arg[0] != 2) {
                                gameoverMsg = "You lose!";
                            }
                            else {
                                gameoverMsg = "No winner!";
                            }
                            Invoke("popMsgBoxForGameover", 4f);
                            hasGameOver = true;

                            // 显示Back按钮
                            gameUI.BackBtnPanel.SetActive(true);
                            gameUI.BackBtnPanel.GetComponent<RectTransform>().anchoredPosition += Vector2.down * 80f;
                        }
                    }

                    index += 7;
                }

                // 执行命令
                if (!grid.isRunning) {
                    grid.isRunning = true;
                    grid.RunCommands();
                }
            }
            else if ((string)data["request"] == "reconnect") {
                StartCoroutine(gameUI.popErrorBar("reconnect success!"));
                isReconnecting = false;
                hasDisConnect = false;
            }
        }
        catch (Exception e) {
            if (!hasGameOver) {
                JsonStr = JsonStr.Trim();
                
                if (e.Message.Trim() == "The remote party closed the WebSocket connection without completing the close handshake.") {
                    hasDisConnect = true;
                }
                else {
                    popMsgBox("receive message failed: " + JsonStr, 1);
                }
            }
            
            Debug.LogError(e);

            return;
        }

        ReceiveFormServer();
    }

    double operateTime; // 千倍的时间
    bool beginCount = false;
    bool showTime = false;
    int curTime;
    int beginShowPoint;
    void Update()
    {
        if (gameUI.gameMode == 1) {
            if (MsgMask.activeSelf && msgState < 3) {
                textTime += Time.deltaTime;
                MsgShowText.text = msgtext[(int)textTime % 3 + msgState * 3];
            }
            else if (beginCount) {
                if (!showTime && operateTime <= (beginShowPoint + 1) * 1000) {
                    curTime = Mathf.FloorToInt((float)operateTime / 1000f);
                    startShowTime();
                    showTime = true;
                    gameUI.TimeText.text = curTime.ToString();
                }
                operateTime -= Time.deltaTime * 1000f;
                if (operateTime <= (curTime) * 1000 && curTime > 0) {
                    curTime -= 1;
                    if (showTime) {
                        gameUI.TimeText.text = curTime.ToString();
                    }
                }
            }

            if (hasDisConnect && !isReconnecting) {
                popMsgBox("Connection failed! Please check your network and reconnect later.", 2);
            }
        }
    }

    void startShowTime()
    {
        StartCoroutine(gameUI.popTimePanel(true));
    }

    public void popMsgBox(string msg, int onClickType)
    {
        HexMapCamera.Locked = true;
        gameUI.enabled = false;

        Transform msgbox = Instantiate(MsgBoxPrefab, MsgCanvas, false);
        msgbox.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
        msgbox.GetChild(0).GetComponent<Text>().text = msg;
        
        if (onClickType == 0) {
            msgbox.GetChild(1).GetComponent<Button>().onClick.AddListener(back);
        }
        else if (onClickType == 1) {
            msgboxStack.Push(msgbox.gameObject);
            msgbox.GetChild(1).GetComponent<Button>().onClick.AddListener(closeSelf);
        }
        else if (onClickType == 2) {
            isReconnecting = true;
            msgboxStack.Push(msgbox.gameObject);
            msgbox.GetChild(1).GetComponent<Button>().onClick.AddListener(ReConnect);
        }
    }

    string gameoverMsg;
    void popMsgBoxForGameover()
    {
        popMsgBox(gameoverMsg, 1);
    }

    public void back()
    {        
        SceneManager.LoadScene(0);
    }

    Stack<GameObject> msgboxStack;
    public void closeSelf()
    {
        Destroy(msgboxStack.Pop());
        HexMapCamera.Locked = false;
        gameUI.enabled = true;
    }

    public async void DisConnect()
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", ct);
        Debug.Log("has close with webSocket.CloseStatus: " + webSocket.CloseStatus);
    }

    // 这个函数在UI界面点击重连的时候使用
    bool isReconnecting = false;
    public async void ReConnect()
    {
        closeSelf();
        Debug.Log("in reconnect");
        StartCoroutine(gameUI.popErrorBar("Reconnecting..."));
        int state = 0;

        try {
            // 关闭原来的连接
            // await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", ct);
            webSocket.Abort();
            Debug.Log("has close with webSocket.CloseStatus: " + webSocket.CloseStatus);
            state = 1;
            webSocket = new ClientWebSocket();
        }
        catch (Exception e) {
            Debug.Log(e);
            Debug.Log("state = " + state.ToString());
        }

        try {
            Uri url;
            if (decodeSuccess) {
                url = new Uri("wss://" + token);
            }
            else {
                url = new Uri("ws://" + token);
            }
            Debug.Log(url);

            await webSocket.ConnectAsync(url, ct);
            state = 2;

            _Start _start = new _Start();
            _start.token = EncodeBase64("utf-8", token);
            _start.request = "reconnect";
            string JsonStr = JsonMapper.ToJson(_start);
            Debug.Log("发送的消息是：" + JsonStr);
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonStr)),
                WebSocketMessageType.Binary, true, ct);
            state = 3;
        }
        catch (Exception e) {
            popMsgBox("Unable to reconnect! Please check your network settings and try again later.", 2);
            Debug.Log(e);
            Debug.Log("state = " + state.ToString());
            return;
        }

        ReceiveFormServer();
    }

    // 销毁前会调用这个函数
    private void OnDestroy()
    {
        Debug.Log("in OnDestroy.");
        if (webSocket != null)
            DisConnect();
    }

    public void Send(string msg)
    {
        _Send _send = new _Send();
        _send.token = token;
        _send.request = "action";
        _send.content = msg;
        SendMessageToServer(JsonMapper.ToJson(_send));
    }

    // 编码函数
    public static string EncodeBase64(string code_type, string code)
    {
        string encode = "";
        try {
            byte[] bytes = Encoding.GetEncoding(code_type).GetBytes(code);
            encode = Convert.ToBase64String(bytes);
        }
        catch {
            encode = code;
        }
        return encode;
    }

    // 解码函数
    public string DecodeBase64(string code_type, string code)
    {
        string decode = "";
        try {
            byte[] bytes = Convert.FromBase64String(code);
            decode = Encoding.GetEncoding(code_type).GetString(bytes);
        }
        catch (Exception e) {
            Debug.Log(e);
            popMsgBox("cannot decode the token!" + code, 1);
            decode = code;
            Debug.Log("cannot decode the token!" + code);
        }
        return decode;
    }

    /*public class Person
	{
		public string Name { get; set; }
		public int Age { get; set; }
		public DateTime Birthday { get; set; }
	}

	public void PersonToJson()
	{
		Person hl = new Person();
		
		hl.Name = "海澜";
		hl.Age = 51;
		hl.Birthday = new DateTime(2018,05,17);
		
		string JsonStr = JsonMapper.ToJson(hl);
		
		Debug.Log(JsonStr);
	}

	public void JsonToPerson()
	{
		string json = @"
		{
			""Name"" : ""海澜"",
			""Age"" : 2018,
			""Birthday"" : ""05/17/2018 00:00:00""
		}";
		
		Person hl = JsonMapper.ToObject<Person>(json);
		
		Debug.Log("JsonToPerson Is Name:" + hl.Name);
	}*/

    /*public void AddCommandsFromMsg(string msg)
    {
        _Receive _receive = JsonMapper.ToObject<_Receive>(msg);
        string content = _receive.content;

    }*/

    public string CommandToMsg(Command C)
    {
        _Base B = new _Base();
        B.player = grid.activeCamp;
        B.round = C.round;
        if (C.type == CommandType.SETUPDECK) {
            B.player = C.arg[0]; // 确保正确，覆盖原来的值
            B.operation_type = "init";
            _InitParam param = new _InitParam();
            param.artifacts = new string[] {
                HexMetrics.atfName[C.arg[1] % 10]
            };
            param.creatures = new string[] {
                HexMetrics.unitName[C.arg[2]],
                HexMetrics.unitName[C.arg[3]],
                HexMetrics.unitName[C.arg[4]]
            };
            B.operation_parameters = param;
        }
        else if (C.type == CommandType.SUMMON) {
            B.operation_type = "summon";
            _SummonParam param = new _SummonParam();
            param.position = new int[] {
                C.arg[2], C.arg[3], 0 - C.arg[2] - C.arg[3]
            };
            param.type = HexMetrics.unitName[C.arg[0]];
            param.level = C.arg[1];
            B.operation_parameters = param;
        }
        else if (C.type == CommandType.MOVE) {
            B.operation_type = "move";
            _MoveParam param = new _MoveParam();
            param.mover = C.arg[0];
            param.position = new int[] {
                C.arg[1], C.arg[2], 0 - C.arg[1] - C.arg[2]
            };
            B.operation_parameters = param;
        }
        else if (C.type == CommandType.PREATK) {
            B.operation_type = "attack";
            _AttackParam param = new _AttackParam();
            param.attacker = C.arg[0];
            param.target = C.arg[1];
            B.operation_parameters = param;
        }
        else if (C.type == CommandType.ATF) {
            B.operation_type = "use";
            if (C.arg[1] == 1 || C.arg[1] == 11 || C.arg[1] == 3 || C.arg[1] == 13 || C.arg[1] == 4 || C.arg[1] == 14) {
                _UseParam01 param = new _UseParam01();
                param.card = C.arg[0];
                param.target = new int[] {
                    C.arg[2], C.arg[3], 0 - C.arg[2] - C.arg[3]
                };
                B.operation_parameters = param;
            }
            else if (C.arg[1] == 2 || C.arg[1] == 12) {
                _UseParam02 param = new _UseParam02();
                param.card = C.arg[0];
                param.target = C.arg[4];
                B.operation_parameters = param;
            }

        }
        else if (C.type == CommandType.ROUNDOVER) {
            B.operation_type = "endround";
            B.operation_parameters = new _EndRoundParam();
        }
        else if (C.type == CommandType.GAMEOVER) {
            B.operation_type = "surrender";
            B.operation_parameters = new _EndRoundParam();
        }
        else Debug.Log("error: send command type unknown: " + C.type);

        return JsonMapper.ToJson(B);
    }

    public class _Base
    {
        public int player;
        public int round;
        public string operation_type;
        public _BaseParam operation_parameters;
    }

    public class _BaseParam
    {

    }

    public class _InitParam : _BaseParam
    {
        public string[] artifacts;
        public string[] creatures;
    }

    public class _SummonParam : _BaseParam
    {
        public int[] position;
        public string type;
        public int level;
    }

    public class _MoveParam : _BaseParam
    {
        public int mover;
        public int[] position;
    }

    public class _AttackParam : _BaseParam
    {
        public int attacker;
        public int target;
    }

    public class _UseParam01 : _BaseParam
    {
        public int card;
        public int[] target;
    }

    public class _UseParam02 : _BaseParam
    {
        public int card;
        public int target;
    }

    public class _EndRoundParam : _BaseParam
    {

    }
}