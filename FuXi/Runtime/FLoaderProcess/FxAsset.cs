using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuXi
{
    public partial class FxAsset : FxAsyncTask
    {
        private enum LoadSteps
        {
            LoadBundle,
            LoadAsset
        }
        private readonly bool m_LoadImmediate;
        private readonly BundleLoader m_BundleLoader;
        private AssetBundleRequest m_BundleRequest;
        private LoadSteps m_LoadStep;
        private readonly Action<FxAsset> m_Completed;

        internal readonly FxReference fxReference;
        internal AssetManifest manifest;
        
#if UNITY_EDITOR
        internal string stackInfo;
#endif
        
        protected readonly string m_FilePath;
        protected readonly Type m_Type;
        public UnityEngine.Object asset;

        internal FxAsset(string path, Type type, bool loadImmediate, Action<FxAsset> callback)
        {
            this.m_FilePath = FuXiManager.ManifestVC.CombineAssetPath(path);
            this.m_Type = type;
            this.m_LoadImmediate = loadImmediate;
            this.m_Completed = callback;
            this.m_BundleLoader = new BundleLoader();
            this.fxReference = new FxReference();
        }
        internal override FTask<FxAsyncTask> Execute()
        {
            base.Execute();
            if (FuXiManager.RuntimeMode == RuntimeMode.Editor) return null;
#if UNITY_EDITOR
            this.stackInfo = StackTraceUtility.ExtractStackTrace();
#endif
            if (!FuXiManager.ManifestVC.TryGetAssetManifest(this.m_FilePath, out this.manifest))
            {
                this.tcs.SetResult(this);
                this.isDone = true;
                this.m_Completed?.Invoke(this);
                AssetCache.Remove(this.m_FilePath);
                return this.tcs;
            }
            this.m_BundleLoader.StartLoad(manifest, this.m_LoadImmediate);
            if (this.m_LoadImmediate)
            {
                if (this.m_BundleLoader.mainLoader.assetBundle != null)
                    this.asset = this.m_BundleLoader.mainLoader.assetBundle.LoadAsset(this.m_FilePath, this.m_Type);
                this.tcs.SetResult(this);
                this.isDone = true;
            }
            this.m_LoadStep = LoadSteps.LoadBundle;
            return this.tcs;
        }

        protected override void Update()
        {
            if (this.isDone) return;
            switch (this.m_LoadStep)
            {
                case LoadSteps.LoadBundle:
                    if (!this.m_BundleLoader.isDone)
                    {
                        this.m_BundleLoader.Update();
                        return;
                    }
                    if (this.m_BundleLoader.mainLoader.assetBundle == null)
                    {
                        this.isDone = true;
                        this.m_Completed?.Invoke(this);
                        this.tcs.SetResult(this);
                        return;
                    }
                    this.m_BundleRequest = this.m_BundleLoader.mainLoader.assetBundle.LoadAssetAsync(this.m_FilePath, this.m_Type);
                    this.m_LoadStep = LoadSteps.LoadAsset;
                    break;
                case LoadSteps.LoadAsset:
                    if (!this.m_BundleRequest.isDone) 
                        return;
                    this.asset = this.m_BundleRequest.asset;
                    this.isDone = true;
                    this.m_Completed?.Invoke(this);
                    this.tcs.SetResult(this);
                    break;
            }
        }

        private void AddReference()
        {
            this.fxReference.AddRef();
        }

        public void Release()
        {
            this.m_BundleLoader?.Release();
            if (!this.fxReference.SubRef()) return;

            AssetCache.Remove(this.m_FilePath);
            this.Dispose();
        }

        protected override void Dispose()
        {
            this.m_BundleLoader?.Dispose();
        }
    }
}