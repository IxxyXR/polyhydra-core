using System;
using System.Diagnostics;
using System.IO;
using Polyhydra.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

// With thanks to BRS Recht:

// Opposite-Lace
// wythoff -p [V,E2F]1F,1e1_0e,0_1f1f,1E dodecahedron | antiview
//
// Ethel
// wythoff -p [V,VE,VF]0_1_2e1e,2F,1_2v2f dodecahedron | antiview
//
// Waffle
// wythoff -p [V,E,F,V2E,VF]0_4_3f4f,2_4_3v3_4v,3E dodecahedron | antiview
//
// Bowtie
// wythoff -p [V,E,F,VE,EF]1_3_4,0_3_4_2e4_1_3e dodecahedron | antiview
//
// Lozenge
// wythoff -p [V,EF]0_1F,1_0f1f,1E dodecahedron | antiview
//
// Hollow
// wythoff -p [V,VF]0_1v1_0v,1v1f,1V dodecahedron | antiview


[CreateAssetMenu(fileName = "AntiprismSettings", menuName = "Polyhydra/AntiprismSettings", order = 1)]
public class AntiprismSettings : BaseSettings
{
    [Header("Base Shape Parameters")]
    public string command = "conway kC";

    public override PolyMesh BuildBaseShape()
    {
        var parts = command.Split(new []{' '}, 2);
        string output = "";
        Process process = new Process();
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = parts[0];
        process.StartInfo.Arguments = parts[1];

        try
        {
            process.Start();
            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Debug.LogError("Run error" + e);
        }
        finally
        {
            process.Dispose();
        }

        using (StringReader reader = new StringReader(output))
        {
            var poly = new PolyMesh(reader);
            return poly;
        }
    }
}



