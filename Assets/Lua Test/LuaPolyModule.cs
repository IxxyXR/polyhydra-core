using Fab.Lua.Core;
using MoonSharp.Interpreter;
using Polyhydra.Core;
using UnityEngine;

[LuaName("poly")]
public class LuaPolyModule : LuaObject, ILuaObjectInitialize
{
    public LuaPolyBuilder builder;

	public void Initialize()
    {
        LuaCustomConverters.RegisterAll();
        builder = GameObject.FindObjectOfType<LuaPolyBuilder>();
    }

	[LuaHelpInfo("cube")]
	public void cube()
    {
        builder.poly = RadialSolids.Build(RadialSolids.RadialPolyType.Prism, 4);
        builder.Build();
    }

    [LuaHelpInfo("Kis")]
    public void kis(float amount)
    {
        builder.poly = builder.poly.Kis(new OpParams(amount));
        builder.Build();
    }

    [LuaHelpInfo("Face Remove")]
    public void faceRemove(Closure func)
    {
        builder.poly = builder.poly.FaceRemove(new OpParams(LuaFilter(func)));
        builder.Build();
    }

    private Filter LuaFilter(Closure func)
    {
        return new (
            p =>
            {
                var face = p.poly.Faces[p.index];
                func.OwnerScript.Globals["index"] = p.index;
                func.OwnerScript.Globals["center"] = face.Centroid;
                func.OwnerScript.Globals["normal"] = face.Normal;
                func.OwnerScript.Globals["role"] = p.poly.FaceRoles[p.index];
                return func.Call().Boolean;
            },
            _ => true
        );
    }
}

