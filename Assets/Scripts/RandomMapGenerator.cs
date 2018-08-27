using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomMapGenerator : MonoBehaviour {

    public int width, height;
    public string seed;
    public bool useRandomSeed;
    [Range(0, 100)]
    public int randomFillPercent;

    private int[,] map;

    // Use this for initialization
    void Start() {
        GenerateMap();
    }

    void GenerateMap() {
        map = new int[width, height];
        RandomFillMap();
    }

    void RandomFillMap() {
        if (useRandomSeed) {
            seed = Time.time.ToString();
        }

        // pseudo random number generator (17% is just a little Dota 2 joke)
        System.Random seventeenPercent = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                map[x, y] = seventeenPercent.Next(0, 100) < randomFillPercent ? 1 : 0;
            }
        }
    }

    private void OnDrawGizmos() {
        if (map != null) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Gizmos.color = map[x, y] == 1 ? Color.black : Color.white;
                    Vector3 position = new Vector3(-width / 2 + x + 0.5f, 0, -height / 2 + y + 0.5f);
                    Gizmos.DrawCube(position, Vector3.one);
                }
            }
        }
    }
}
