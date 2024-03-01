using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public struct GeoJsonRoot
{
    public string type;
    public string name;
    public Crs crs;
    public Features[] features;
}

[Serializable]
public struct Crs
{
    public string type;
    [FormerlySerializedAs("properties")] public CrsProperties crsProperties;
}

[Serializable]
public struct CrsProperties
{
    public string name;
}

[Serializable]
public struct Features
{
    public string type;
    public Properties properties;
    public Geometry geometry;
}

[Serializable]
public struct Properties
{
    public int gid;
    public float b_ref;
    public float h_storey;
    public object h_metres;
    public int block_no;
    public object phase_name;
}

[Serializable]
public struct Geometry
{
    public string type;
    public List<List<List<List<float>>>> coordinates;
}