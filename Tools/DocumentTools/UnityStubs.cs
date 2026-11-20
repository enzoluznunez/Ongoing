using System;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object message) => Console.Error.WriteLine(message);
        public static void LogWarning(object message) => Console.Error.WriteLine(message);
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }
}
