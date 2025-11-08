#if !VOL_FX

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

//  Dither © NullTale - https://x.com/NullTale
namespace VolFx
{
    public class DitherFx : ScriptableRendererFeature
    {
        protected static List<ShaderTagId> k_ShaderTags;

        [Tooltip("When to execute")]
        public RenderPassEvent _event  = RenderPassEvent.AfterRenderingPostProcessing;
        
        public DitherPass _pass;

        [NonSerialized]
        public PassExecution _execution;

        // =======================================================================
        public class PassExecution : ScriptableRenderPass
        {
            public DitherFx _owner;

            private class PassData
            {
                public Material material;
                public TextureHandle source;
            }

            // =======================================================================
            public void Init()
            {
                renderPassEvent = _owner._event;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                // Validate the pass
                _owner._pass.Validate();
                if (_owner._pass.IsActive == false)
                    return;

                // Get URP resource data
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                // Get source texture (camera color)
                TextureHandle source = resourceData.cameraColor;

                // Create destination texture
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;

                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph,
                    desc,
                    "_DitherOutput",
                    false);

                // Invoke the dither pass using RenderGraph
                _owner._pass.InvokeRenderGraph(renderGraph, _owner.name, source, destination, frameData);

                // Update camera color to the output
                resourceData.cameraColor = destination;
            }
        }

        // =======================================================================
        public override void Create()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            _execution = new PassExecution() { _owner = this };
            _execution.Init();

            if (_pass != null)
                _pass._init();

            if (k_ShaderTags == null)
            {
                k_ShaderTags = new List<ShaderTagId>(new[]
                {
                    new ShaderTagId("SRPDefaultUnlit"),
                    new ShaderTagId("UniversalForward"),
                    new ShaderTagId("UniversalForwardOnly")
                });
            }
        }
        
        private void Reset()
        {
#if UNITY_EDITOR
            if (_pass != null)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(_pass);
                UnityEditor.AssetDatabase.SaveAssets();
                _pass = null;
            }
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;
#if UNITY_EDITOR
            if (_pass == null)
                return;
#endif
            renderer.EnqueuePass(_execution);
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (_pass != null)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(_pass);
                UnityEditor.AssetDatabase.SaveAssets();
                _pass = null;
            }
#endif
        }
    }
}

#endif