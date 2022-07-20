using Fab.Lua.Core;
using Polyhydra.Core;
using UnityEngine;

[LuaName("poly")]
public class LuaPolyModule : LuaObject, ILuaObjectInitialize
{
    public LuaPolyBuilder builder;

	public void Initialize()
    {
        builder = GameObject.FindObjectOfType<LuaPolyBuilder>();
    }

	[LuaHelpInfo("cube")]
	public void cube()
    {
        builder.poly = RadialSolids.Build(RadialSolids.RadialPolyType.Prism, 4);
        builder.Build();
    }

    [LuaHelpInfo("kis")]
    public void kis(float amount)
    {
        builder.poly = builder.poly.Kis(new OpParams(amount));
        builder.Build();
    }
}

