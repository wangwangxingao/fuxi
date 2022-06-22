using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace FuXi.Editor
{
    public class FxEditorScene : FxScene
    {
        internal static FxEditorScene CreateEditorScene(string path, bool addition, bool immediate)
        { return new FxEditorScene(path, addition, immediate); }
        
        FxEditorScene(string path, bool additive, bool immediate) : base(path, additive, immediate) { }
        internal override Task<FxAsyncTask> Execute()
        {
            base.Execute();
            if (null != FxManager.ManifestVC && !FxManager.ManifestVC.TryGetAssetManifest(this.m_ScenePath, out _))
            {
                this.tcs.SetResult(this);
                this.isDone = true;
            }
            else
            {
                RefreshRef(this);
                if (this.m_Immediate)
                {
                    EditorSceneManager.LoadSceneInPlayMode(this.m_ScenePath, new LoadSceneParameters(this.m_LoadMode));
                    RefreshRef(this);
                    this.tcs.SetResult(this);
                    this.isDone = true;
                }
                else
                    this.m_Operation = EditorSceneManager.LoadSceneAsyncInPlayMode(this.m_ScenePath,
                        new LoadSceneParameters(this.m_LoadMode));
            }
            return this.tcs.Task;
        }

        protected override void Update()
        {
            if (this.isDone) return;
            if (!this.m_Operation.isDone) return;
            
            this.tcs.SetResult(this);
            this.isDone = true;
        }
    }
}