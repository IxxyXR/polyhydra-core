using Fab.Lua.Core;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Fab.Lua.ConsoleSample
{
	internal static class RegisterAssembly
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void Register()
		{
			UserData.RegisterAssembly();
			LuaEnvironment.Registry.RegisterAssembly();
		}
	}
}
