using UnityEngine;
using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;

/// <summary>
/// These are custom converters for Vector2 and Vector3 structs.
///
/// Add the following call to your code:
/// LuaCustomConverters.RegisterAll();
///
/// To create Vector2 in lua:
/// position = {1.0, 1.0}
///
/// To Vector3 in lua:
/// position = {1.0, 1.0, 1.0}
///
/// </summary>
public static class LuaCustomConverters {

    public static void RegisterAll() {

        // Vector 2

        Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector2),
            dynVal => {
                Table table = dynVal.Table;
                float x = (float)((Double)table["x"]);
                float y = (float)((Double)table["y"]);
                return new Vector2(x, y);
            }
        );
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vector2>(
            (script, vector) => {
                DynValue x = DynValue.NewNumber((double)vector.x);
                DynValue y = DynValue.NewNumber((double)vector.y);
                DynValue dynVal = DynValue.NewTable(script, new DynValue[] { x=x, y=y});
                return dynVal;
            }
        );

        // Vector3

        Script.GlobalOptions.CustomConverters.SetScriptToClrCustomConversion(DataType.Table, typeof(Vector3),
            dynVal => {
                Table table = dynVal.Table;
                float x = (float)((Double)table["x"]);
                float y = (float)((Double)table["y"]);
                float z = (float)((Double)table["z"]);
                return new Vector3(x, y, z);
            }
        );
        Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<Vector3>(
            (script, vector) => {
                DynValue dynVal = DynValue.FromObject(script, new Dictionary<string,DynValue>
                {
                    {"x", DynValue.NewNumber((double)vector.x)},
                    {"y", DynValue.NewNumber((double)vector.y)},
                    {"z", DynValue.NewNumber((double)vector.z)},
                });
                return dynVal;
            }
        );

    }

}