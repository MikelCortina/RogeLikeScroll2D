using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SideZoneManager : MonoBehaviour
{
    [Serializable]
    public class ZoneConfig
    {
        public string name;
        public List<int> allowedWaveSpaceValues = new List<int>();
        public bool allowAny = false;
    }

    [Header("Side zones (camera-based) settings")]
    public bool useSideZones = true;
    [Range(0.01f, 0.45f)]
    public float sideWidthViewport = 0.18f;
    [Tooltip("Cuántas zonas verticales por lado (división).")]
    public int zonesPerSide = 4;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que el spawn ocurra en el lado izquierdo (0 = siempre derecho, 1 = siempre izquierdo)")]
    public float leftSpawnChance = 0.5f;

    [Header("Side zones offsets")]
    [Range(0f, 1f)] public float leftOutsideOffset = 0.08f;
    [Range(0f, 2f)] public float leftVerticalOffset = 0.08f;
    [Range(0f, 1f)] public float rightOutsideOffset = 0.08f;
    [Range(0f, 2f)] public float rightVerticalOffset = 0.08f;

    [Header("Zonas configurables por inspector (left/right)")]
    public List<ZoneConfig> leftZoneConfigs = new List<ZoneConfig>();
    public List<ZoneConfig> rightZoneConfigs = new List<ZoneConfig>();

    [Header("Top zones (camera-based) settings")]
    public bool useTopZones = true;
    [Range(0.01f, 0.45f)]
    public float topWidthViewport = 0.18f;
    [Tooltip("Cuántas columnas de spawn encima de la cámara.")]
    public int topZonesColumns = 4;

    [Header("Top zones offsets")]
    [Range(0f, 1f)] public float topOutsideOffset = 0.08f;
    [Range(0f, 2f)] public float topVerticalOffset = 0.08f;

    public List<ZoneConfig> topZoneConfigs = new List<ZoneConfig>();

    [Header("Debug / visualization")]
    public Camera debugCamera;

    private Camera mainCam;

    private void OnValidate()
    {
        SyncZoneConfigs(leftZoneConfigs);
        SyncZoneConfigs(rightZoneConfigs);
        SyncTopZoneConfigs();
    }

    private void Awake()
    {
        mainCam = debugCamera != null ? debugCamera : Camera.main;
    }

    private void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    private void SyncZoneConfigs(List<ZoneConfig> list)
    {
        if (list == null) return;
        while (list.Count < zonesPerSide)
            list.Add(new ZoneConfig() { name = $"Zone {list.Count}" });
        while (list.Count > zonesPerSide && zonesPerSide > 0)
            list.RemoveAt(list.Count - 1);
    }

    private void SyncTopZoneConfigs()
    {
        if (topZoneConfigs == null) return;
        while (topZoneConfigs.Count < topZonesColumns)
            topZoneConfigs.Add(new ZoneConfig() { name = $"Top Zone {topZoneConfigs.Count}" });
        while (topZoneConfigs.Count > topZonesColumns && topZonesColumns > 0)
            topZoneConfigs.RemoveAt(topZoneConfigs.Count - 1);
    }

    // Public API used by WaveManager
    public bool TryGetSideSpawnPosition(float waveSpace, float spawnRandomRadius, out Vector3 outWorldPos, out bool usedOtherSide)
    {
        outWorldPos = Vector3.zero;
        usedOtherSide = false;

        if (!useSideZones || zonesPerSide <= 0)
            return false;

        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null)
            return false;

        bool chooseLeft = UnityEngine.Random.value <= leftSpawnChance;

        List<int> eligibleZoneIndices = new List<int>();
        List<ZoneConfig> configs = chooseLeft ? leftZoneConfigs : rightZoneConfigs;

        for (int i = 0; i < zonesPerSide; i++)
        {
            if (i >= configs.Count) continue;
            if (ZoneConfigAllowsWaveSpace(configs[i], waveSpace))
                eligibleZoneIndices.Add(i);
        }

        int zoneIndex = -1;

        if (eligibleZoneIndices.Count > 0)
        {
            zoneIndex = eligibleZoneIndices[UnityEngine.Random.Range(0, eligibleZoneIndices.Count)];
        }
        else
        {
            // intentar el otro lado
            List<ZoneConfig> otherConfigs = chooseLeft ? rightZoneConfigs : leftZoneConfigs;
            List<int> otherEligible = new List<int>();
            for (int i = 0; i < zonesPerSide; i++)
            {
                if (i >= otherConfigs.Count) continue;
                if (ZoneConfigAllowsWaveSpace(otherConfigs[i], waveSpace))
                    otherEligible.Add(i);
            }

            if (otherEligible.Count > 0)
            {
                chooseLeft = !chooseLeft;
                configs = otherConfigs;
                zoneIndex = otherEligible[UnityEngine.Random.Range(0, otherEligible.Count)];
                usedOtherSide = true;
            }
            else
            {
                return false;
            }
        }

        Vector3 worldPos = GetRandomPositionInSideZoneInternal(cam, chooseLeft, zoneIndex);
        outWorldPos = worldPos + (Vector3)(UnityEngine.Random.insideUnitCircle * spawnRandomRadius);
        return true;
    }

    public Vector3 GetRandomPositionInSideZone(bool left, int zoneIndex)
    {
        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) return transform.position;
        return GetRandomPositionInSideZoneInternal(cam, left, zoneIndex);
    }

    private Vector3 GetRandomPositionInSideZoneInternal(Camera cam, bool left, int zoneIndex)
    {
        float xMin, xMax, vOffset;
        if (left)
        {
            xMin = -leftOutsideOffset - sideWidthViewport;
            xMax = -leftOutsideOffset;
            vOffset = leftVerticalOffset;
        }
        else
        {
            xMin = 1f + rightOutsideOffset;
            xMax = 1f + rightOutsideOffset + sideWidthViewport;
            vOffset = rightVerticalOffset;
        }

        float zoneHeight = 1f / Mathf.Max(1, zonesPerSide);
        float yMin = zoneIndex * zoneHeight;
        float yMax = (zoneIndex + 1) * zoneHeight;

        float vx = UnityEngine.Random.Range(xMin, xMax);
        float vy = UnityEngine.Random.Range(yMin + 0.01f, Mathf.Max(yMin + 0.01f, yMax - 0.01f));

        Vector3 viewPoint = new Vector3(vx, vy, Mathf.Abs(cam.transform.position.z));

        if (cam.orthographic)
            viewPoint.z = cam.nearClipPlane + 1f;
        else
            viewPoint.z = Mathf.Abs(cam.transform.position.z);

        Vector3 world = cam.ViewportToWorldPoint(viewPoint);
        world.z = 0f;
        world.y += vOffset;
        return world;
    }

    // Top zone spawn
    public bool TryGetTopSpawnPosition(float waveSpace, float spawnRandomRadius, out Vector3 outWorldPos)
    {
        outWorldPos = Vector3.zero;

        if (!useTopZones || topZonesColumns <= 0) return false;

        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) return false;

        List<int> eligibleColumns = new List<int>();
        for (int i = 0; i < topZonesColumns; i++)
        {
            if (i >= topZoneConfigs.Count) continue;
            if (ZoneConfigAllowsWaveSpace(topZoneConfigs[i], waveSpace))
                eligibleColumns.Add(i);
        }

        if (eligibleColumns.Count == 0) return false;

        int colIndex = eligibleColumns[UnityEngine.Random.Range(0, eligibleColumns.Count)];
        Vector3 worldPos = GetRandomPositionInTopZone(cam, colIndex);
        outWorldPos = worldPos + (Vector3)(UnityEngine.Random.insideUnitCircle * spawnRandomRadius);
        return true;
    }

    private Vector3 GetRandomPositionInTopZone(Camera cam, int colIndex)
    {
        float zoneWidth = 1f / Mathf.Max(1, topZonesColumns);
        float xMin = colIndex * zoneWidth;
        float xMax = (colIndex + 1) * zoneWidth;

        float vx = UnityEngine.Random.Range(xMin + 0.01f, Mathf.Max(xMin + 0.01f, xMax - 0.01f));
        float vy = 1f + topOutsideOffset;

        Vector3 viewPoint = new Vector3(vx, vy, Mathf.Abs(cam.transform.position.z));

        if (cam.orthographic)
            viewPoint.z = cam.nearClipPlane + 1f;
        else
            viewPoint.z = Mathf.Abs(cam.transform.position.z);

        Vector3 world = cam.ViewportToWorldPoint(viewPoint);
        world.z = 0f;
        world.y += topVerticalOffset;
        return world;
    }

    public bool ZoneConfigAllowsWaveSpace(ZoneConfig cfg, float waveSpace)
    {
        if (cfg == null) return false;
        if (cfg.allowAny) return true;
        if (cfg.allowedWaveSpaceValues == null || cfg.allowedWaveSpaceValues.Count == 0) return false;

        int rounded = Mathf.RoundToInt(waveSpace);
        return cfg.allowedWaveSpaceValues.Contains(rounded);
    }

    private void OnDrawGizmosSelected()
    {
        if (!useSideZones && !useTopZones) return;

        Camera cam = mainCam != null ? mainCam : (debugCamera != null ? debugCamera : Camera.main);
        if (cam == null) return;

        // Side zones
        for (int side = 0; side <= 1; side++)
        {
            bool left = side == 0;

            float xMin, xMax, vOffset;
            if (left)
            {
                xMin = -leftOutsideOffset - sideWidthViewport;
                xMax = -leftOutsideOffset;
                vOffset = leftVerticalOffset;
            }
            else
            {
                xMin = 1f + rightOutsideOffset;
                xMax = 1f + rightOutsideOffset + sideWidthViewport;
                vOffset = rightVerticalOffset;
            }

            float zoneHeight = 1f / Mathf.Max(1, zonesPerSide);

            for (int z = 0; z < zonesPerSide; z++)
            {
                float yMin = z * zoneHeight;
                float yMax = (z + 1) * zoneHeight;

                Vector3 bl = cam.ViewportToWorldPoint(new Vector3(xMin, yMin, Mathf.Abs(cam.transform.position.z)));
                Vector3 tr = cam.ViewportToWorldPoint(new Vector3(xMax, yMax, Mathf.Abs(cam.transform.position.z)));
                bl.z = 0f; tr.z = 0f;

                bl.y += vOffset;
                tr.y += vOffset;

                Vector3 center = (bl + tr) / 2f;
                Vector3 size = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 0.01f);

                List<ZoneConfig> configs = left ? leftZoneConfigs : rightZoneConfigs;
                bool allows = z < configs.Count && configs[z] != null && (configs[z].allowAny || (configs[z].allowedWaveSpaceValues != null && configs[z].allowedWaveSpaceValues.Count > 0));

                if (allows)
                    Gizmos.DrawCube(center, size * 0.99f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(center, size);
            }
        }

        // Top zones
        if (useTopZones)
        {
            float zoneWidth = 1f / Mathf.Max(1, topZonesColumns);
            for (int i = 0; i < topZonesColumns; i++)
            {
                float xMin = i * zoneWidth;
                float xMax = (i + 1) * zoneWidth;
                float yMin = 1f + topOutsideOffset;
                float yMax = yMin + topWidthViewport;

                Vector3 bl = cam.ViewportToWorldPoint(new Vector3(xMin, yMin, Mathf.Abs(cam.transform.position.z)));
                Vector3 tr = cam.ViewportToWorldPoint(new Vector3(xMax, yMax, Mathf.Abs(cam.transform.position.z)));
                bl.z = 0f; tr.z = 0f;

                bl.y += topVerticalOffset;
                tr.y += topVerticalOffset;

                Vector3 center = (bl + tr) / 2f;
                Vector3 size = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 0.01f);

                bool allows = i < topZoneConfigs.Count && topZoneConfigs[i] != null && (topZoneConfigs[i].allowAny || (topZoneConfigs[i].allowedWaveSpaceValues != null && topZoneConfigs[i].allowedWaveSpaceValues.Count > 0));
                if (allows)
                    Gizmos.DrawCube(center, size * 0.99f);

                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
