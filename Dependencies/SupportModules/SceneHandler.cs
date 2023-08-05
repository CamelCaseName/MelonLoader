using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using System.Runtime.InteropServices;
#if SM_Il2Cpp
using UnityEngine.Events;
#endif

namespace MelonLoader.Support
{
    public static class SceneSupport
    {
        public static unsafe string GetName(this Scene scene)
        {
#if SM_Il2Cpp
            var nativeSceneClass = Il2CppClassPointerStore.GetNativeClassPointer(typeof(Scene));
            if (nativeSceneClass == IntPtr.Zero)
            {
                MelonDebug.Error("scene.get_name is missing and the workaround failed (class pointer was zero)");
                return "scene names stripped";
            }
            var nativeMethod = IL2CPP.il2cpp_class_get_method_from_name(nativeSceneClass, "get_name", 0);
            if (nativeMethod == IntPtr.Zero)
            {
                MelonDebug.Error("scene.get_name is missing and the workaround failed, all workarounds failed (method pointer for Scene.get_name() was zero)");
                return "scene names stripped";
            }
            IntPtr* ptr = null;
            IntPtr error = IntPtr.Zero;
            var sceneHandle = GCHandle.Alloc(scene.handle, GCHandleType.Pinned);
            if (sceneHandle.AddrOfPinnedObject() == IntPtr.Zero)
            {
                MelonDebug.Error("scene couldn't be pinned");
                return "scene names stripped";
            }
            var nativeResult = IL2CPP.il2cpp_runtime_invoke(nativeMethod, sceneHandle.AddrOfPinnedObject(), (void**)ptr, ref error);
            sceneHandle.Free();
            if (nativeResult == IntPtr.Zero)
            {
                MelonDebug.Error("il2cpp_runtime_invoke on the native Scene.get_name failed (result pointer was zero)");
                return "scene names stripped";
            }
            Il2CppException.RaiseExceptionIfNecessary(error);
            return IL2CPP.Il2CppStringToManaged(nativeResult);
#else
            return scene.name;
#endif
        }
    }

    internal static class SceneHandler
    {
        internal class SceneInitEvent
        {
            internal int buildIndex;
            internal string name;
            internal bool wasLoadedThisTick;
        }

        private static Queue<SceneInitEvent> scenesLoaded = new Queue<SceneInitEvent>();

        internal static void Init()
        {
            try
            {
#if SM_Il2Cpp
                var a = new Action<Scene, LoadSceneMode>(OnSceneLoad);
                var d = DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(a);
                SceneManager.sceneLoaded = (
                    ReferenceEquals(SceneManager.sceneLoaded, null)
                    ? d
                    : Il2CppSystem.Delegate.Combine(SceneManager.sceneLoaded, (UnityAction<Scene, LoadSceneMode>)new Action<Scene, LoadSceneMode>(OnSceneLoad)).Cast<UnityAction<Scene, LoadSceneMode>>()
                    );
#else
                SceneManager.sceneLoaded += OnSceneLoad;
#endif
            }
            catch (Exception ex) { MelonLogger.Error($"SceneManager.sceneLoaded override failed: {ex}"); }
            MelonDebug.Msg("[Il2Cpp SupportModule] SceneManager.sceneLoaded hooked or initialized");

            try
            {
#if SM_Il2Cpp
                SceneManager.sceneUnloaded = (
                    ReferenceEquals(SceneManager.sceneUnloaded, null)
                    ? new Action<Scene>(OnSceneUnload)
                    : Il2CppSystem.Delegate.Combine(SceneManager.sceneUnloaded, (UnityAction<Scene>)new Action<Scene>(OnSceneUnload)).Cast<UnityAction<Scene>>()
                    );
#else
                SceneManager.sceneUnloaded += OnSceneUnload;
#endif
            }
            catch (Exception ex) { MelonLogger.Error($"SceneManager.sceneUnloaded override failed: {ex}"); }
            MelonDebug.Msg("[Il2Cpp SupportModule] SceneManager.sceneUnloaded hooked or initialized");
        }

        private static void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            if (Main.obj == null)
                SM_Component.Create();

            if (ReferenceEquals(scene, null))
                return;

            string name = scene.GetName();

            MelonDebug.Msg("scene " + name + " loaded as " + mode.ToString());

            Main.Interface.OnSceneWasLoaded(scene.buildIndex, name);
            scenesLoaded.Enqueue(new SceneInitEvent { buildIndex = scene.buildIndex, name = name });
        }

        private static void OnSceneUnload(Scene scene)
        {
            if (ReferenceEquals(scene, null))
                return;

            MelonDebug.Msg("scene unloaded");
            Main.Interface.OnSceneWasUnloaded(scene.buildIndex, scene.GetName());
        }

        internal static void OnUpdate()
        {
            if (scenesLoaded.Count > 0)
            {
                Queue<SceneInitEvent> requeue = new Queue<SceneInitEvent>();
                SceneInitEvent evt = null;
                while ((scenesLoaded.Count > 0) && ((evt = scenesLoaded.Dequeue()) != null))
                {
                    if (evt.wasLoadedThisTick)
                        Main.Interface.OnSceneWasInitialized(evt.buildIndex, evt.name);
                    else
                    {
                        evt.wasLoadedThisTick = true;
                        requeue.Enqueue(evt);
                    }
                }
                while ((requeue.Count > 0) && ((evt = requeue.Dequeue()) != null))
                    scenesLoaded.Enqueue(evt);
            }
        }
    }
}
