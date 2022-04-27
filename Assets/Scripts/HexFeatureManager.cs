using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexFeatureCollection
{
    public Transform[] prefabs;
    public Transform Pick(float choice)
    {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}

public class HexFeatureManager : MonoBehaviour
{
    public Transform container;
    public HexFeatureCollection[] 
        feature01Collections, feature02Collections, feature03Collections;
    public Transform[] special;

    public void Awake()
    {
        
    }
    public void Clear()
    {
        if (container) {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply()
    {

    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        if (cell.IsSpecial) {
            return;
        }

        HexHash hash = HexMetrics.SampleHashGrid(position);

        Transform prefab = PickPrefab( // d负责在一族的某level的prefabs集里选择哪一个
            feature01Collections, cell.Feature01Level, hash.a, hash.d
        ); 
        Transform otherPrefab = PickPrefab(
            feature02Collections, cell.Feature02Level, hash.b, hash.d
        );

        float usedHash = hash.a;
        if (prefab) {
            if (otherPrefab && hash.b < hash.a) {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab) {
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(
            feature03Collections, cell.Feature03Level, hash.c, hash.d
        );
        if (prefab) {
            if (otherPrefab && hash.c < usedHash) {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab) {
            prefab = otherPrefab;
        }
        else {
            return;
        }

        if (prefab) {
            Transform instance = Instantiate(prefab);
            instance.localPosition = HexMetrics.Perturb(position);
            instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);//e决定朝向
            instance.SetParent(container, false);
        }
    }
    public void AddSpecialFeature (HexCell cell, Vector3 position)
    {
        if (cell.SpecialIndex <= 0 || cell.SpecialIndex > special.Length) return;
        if (!special[cell.SpecialIndex - 1]) return;

        Transform instance = Instantiate(special[cell.SpecialIndex - 1]);
        instance.localPosition = HexMetrics.Perturb(position);
        HexHash hash = HexMetrics.SampleHashGrid(position);
        //instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        if (cell.SpecialIndex == 1 || cell.SpecialIndex == 3) instance.localRotation = Quaternion.Euler(0f, 90, 0f);
        else if (cell.SpecialIndex == 2 || cell.SpecialIndex == 4) instance.localRotation = Quaternion.Euler(0f, -90, 0f);
        else instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container, false);

        // 告诉cell他拥有哪一类的建筑物
        cell.specialFeature = instance;
    }

    Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0) {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++) {
                if (hash < thresholds[i]) {
                    if (thresholds.Length - 1 - i >= collection.Length) return null; // 若不存在则返回空
                    return collection[thresholds.Length - 1 - i].Pick(choice);
                }
            }
        }
        return null;
    }
}
