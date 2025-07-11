using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Animation
{
    internal class MeshPreviewTool : BaseTool
    {
        [SerializeField]
        private Material m_Material;
        private List<SpriteCache> m_Sprites;
        private IMeshPreviewBehaviour m_DefaultPreviewBehaviour = new DefaultPreviewBehaviour();

        public IMeshPreviewBehaviour previewBehaviourOverride { get; set; }

        public override IMeshPreviewBehaviour previewBehaviour
        {
            get
            {
                if (previewBehaviourOverride != null)
                    return previewBehaviourOverride;

                return m_DefaultPreviewBehaviour;
            }
        }

        internal override void OnCreate()
        {
            m_Material = new Material(Shader.Find("Hidden/SkinningModule-GUITextureClip"));
            m_Material.hideFlags = HideFlags.DontSave;
        }

        internal override void OnDestroy()
        {
            base.OnDestroy();

            Debug.Assert(m_Material != null);

            DestroyImmediate(m_Material);
        }

        protected override void OnActivate()
        {
            m_Sprites = new List<SpriteCache>(skinningCache.GetSprites());

            DirtyMeshesAll();

            skinningCache.events.meshChanged.AddListener(MeshChanged);
            skinningCache.events.characterPartChanged.AddListener(CharacterPartChanged);
            skinningCache.events.skeletonPreviewPoseChanged.AddListener(SkeletonChanged);
            skinningCache.events.skeletonBindPoseChanged.AddListener(SkeletonChanged);
            skinningCache.events.skinningModeChanged.AddListener(SkinningModuleModeChanged);
            skinningCache.events.boneColorChanged.AddListener(BoneColorChanged);
        }

        protected override void OnDeactivate()
        {
            skinningCache.events.meshChanged.RemoveListener(MeshChanged);
            skinningCache.events.skeletonPreviewPoseChanged.RemoveListener(SkeletonChanged);
            skinningCache.events.skeletonBindPoseChanged.RemoveListener(SkeletonChanged);
            skinningCache.events.skinningModeChanged.RemoveListener(SkinningModuleModeChanged);
            skinningCache.events.boneColorChanged.RemoveListener(BoneColorChanged);
        }

        protected override void OnGUI()
        {
            Prepare();

            if (Event.current.type == EventType.Repaint)
                DrawSpriteMeshes();
        }

        public void DrawOverlay()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (skinningCache.mode == SkinningMode.SpriteSheet)
            {
                foreach (SpriteCache sprite in m_Sprites)
                {
                    if (previewBehaviour.Overlay(sprite))
                        DrawSpriteMesh(sprite);
                }
            }
            else
            {
                CharacterCache character = skinningCache.character;
                Debug.Assert(character != null);

                CharacterPartCache[] parts = character.parts;
                foreach (CharacterPartCache part in parts)
                {
                    if (part.isVisible && previewBehaviour.Overlay(part.sprite))
                        DrawSpriteMesh(part.sprite);
                }
            }
        }

        public void OverlayWireframe()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            foreach (SpriteCache sprite in m_Sprites)
                if (previewBehaviour.OverlayWireframe(sprite))
                    DrawWireframe(sprite);
        }

        private void CharacterPartChanged(CharacterPartCache characterPart)
        {
            MeshPreviewCache meshPreview = characterPart.sprite.GetMeshPreview();
            Debug.Assert(meshPreview != null);
            meshPreview.SetSkinningDirty();
        }

        private void MeshChanged(MeshCache mesh)
        {
            MeshPreviewCache meshPreview = mesh.sprite.GetMeshPreview();
            Debug.Assert(meshPreview != null);
            meshPreview.SetMeshDirty();
        }

        private void SkeletonChanged(SkeletonCache skeleton)
        {
            DirtySkinningAll();
        }

        private void SkinningModuleModeChanged(SkinningMode mode)
        {
            DirtyMeshesAll();
        }

        private void BoneColorChanged(BoneCache bone)
        {
            DirtyColorsAll();
        }

        private void DirtyColorsAll()
        {
            foreach (SpriteCache sprite in m_Sprites)
            {
                MeshPreviewCache meshPreview = sprite.GetMeshPreview();

                if (meshPreview != null)
                    meshPreview.SetColorsDirty();
            }
        }

        private void DirtyMeshesAll()
        {
            foreach (SpriteCache sprite in m_Sprites)
            {
                MeshPreviewCache meshPreview = sprite.GetMeshPreview();

                if (meshPreview != null)
                    meshPreview.SetMeshDirty();
            }
        }

        private void DirtySkinningAll()
        {
            foreach (SpriteCache sprite in m_Sprites)
            {
                MeshPreviewCache meshPreview = sprite.GetMeshPreview();
                Debug.Assert(meshPreview != null);
                meshPreview.SetSkinningDirty();
            }
        }

        private void Prepare()
        {
            foreach (SpriteCache sprite in m_Sprites)
            {
                MeshPreviewCache meshPreview = sprite.GetMeshPreview();
                Debug.Assert(meshPreview != null);
                meshPreview.enableSkinning = true;
                meshPreview.Prepare();
            }
        }

        private void DrawDefaultSpriteMeshes()
        {
            Debug.Assert(Event.current.type == EventType.Repaint);

            if (skinningCache.mode == SkinningMode.SpriteSheet)
            {
                foreach (SpriteCache sprite in m_Sprites)
                    DrawDefaultSpriteMesh(sprite);
            }
            else
            {
                CharacterCache character = skinningCache.character;
                Debug.Assert(character != null);

                CharacterPartCache[] parts = character.parts;
                foreach (CharacterPartCache part in parts)
                {
                    if (part.isVisible)
                        DrawDefaultSpriteMesh(part.sprite);
                }
            }
        }

        private void DrawDefaultSpriteMesh(SpriteCache sprite)
        {
            Debug.Assert(m_Material != null);

            MeshPreviewCache meshPreview = sprite.GetMeshPreview();
            MeshCache meshCache = sprite.GetMesh();
            SkeletonCache skeleton = skinningCache.GetEffectiveSkeleton(sprite);

            Debug.Assert(meshPreview != null);

            if (meshPreview.canSkin == false || skeleton.isPosePreview == false)
            {
                m_Material.mainTexture = meshCache.textureDataProvider.texture;
                m_Material.SetFloat("_Opacity", 1f);
                m_Material.SetFloat("_VertexColorBlend", 0f);
                m_Material.color = new Color(1f, 1f, 1f, 1f);

                DrawingUtility.DrawMesh(meshPreview.defaultMesh, m_Material, sprite.GetLocalToWorldMatrixFromMode());
            }
        }

        private void DrawSpriteMeshes()
        {
            Debug.Assert(Event.current.type == EventType.Repaint);

            if (skinningCache.mode == SkinningMode.SpriteSheet)
            {
                foreach (SpriteCache sprite in m_Sprites)
                {
                    if (previewBehaviour.Overlay(sprite))
                        continue;

                    DrawSpriteMesh(sprite);
                }
            }
            else
            {
                CharacterCache character = skinningCache.character;
                Debug.Assert(character != null);

                CharacterPartCache[] parts = character.parts;
                SpriteCache selected = skinningCache.selectedSprite;
                bool selectedVisible = false;
                foreach (CharacterPartCache part in parts)
                {
                    if (previewBehaviour.Overlay(part.sprite))
                        continue;

                    if (part.sprite == selected)
                        selectedVisible = part.isVisible;
                    else if (part.isVisible)
                        DrawSpriteMesh(part.sprite);
                }

                if (selectedVisible && selected != null)
                    DrawSpriteMesh(selected);
            }
        }

        private void DrawSpriteMesh(SpriteCache sprite)
        {
            float weightMapOpacity = previewBehaviour.GetWeightMapOpacity(sprite);
            DrawSpriteMesh(sprite, weightMapOpacity);

            if (previewBehaviour.DrawWireframe(sprite))
                DrawWireframe(sprite);
        }

        private void DrawSpriteMesh(SpriteCache sprite, float weightMapOpacity)
        {
            Debug.Assert(m_Material != null);

            MeshPreviewCache meshPreview = sprite.GetMeshPreview();
            MeshCache meshCache = sprite.GetMesh();

            Debug.Assert(meshPreview != null);

            if (meshPreview.mesh == null || meshPreview.mesh.vertexCount == 0)
            {
                DrawDefaultSpriteMesh(sprite);
            }
            else
            {
                m_Material.mainTexture = meshCache.textureDataProvider.texture;
                m_Material.SetFloat("_Opacity", 1f);
                m_Material.SetFloat("_VertexColorBlend", weightMapOpacity);

                m_Material.color = Color.white;

                DrawingUtility.DrawMesh(meshPreview.mesh, m_Material, sprite.GetLocalToWorldMatrixFromMode());
            }
        }

        private void DrawSelectedSpriteWeightMap()
        {
            SpriteCache selectedSprite = skinningCache.selectedSprite;

            if (selectedSprite != null)
            {
                float opacity = GetWeightOpacityFromCurrentTool();

                if (opacity > 0f)
                    DrawSpriteMesh(selectedSprite, opacity);
            }
        }

        private float GetWeightOpacityFromCurrentTool()
        {
            return IsWeightTool() ? VisibilityToolSettings.meshOpacity : 0f;
        }

        private bool IsWeightTool()
        {
            BaseTool currentTool = skinningCache.selectedTool;

            if (currentTool == skinningCache.GetTool(Tools.WeightSlider) ||
                currentTool == skinningCache.GetTool(Tools.WeightBrush) ||
                currentTool == skinningCache.GetTool(Tools.BoneInfluence) ||
                currentTool == skinningCache.GetTool(Tools.SpriteInfluence) ||
                currentTool == skinningCache.GetTool(Tools.GenerateWeights))
                return true;

            return false;
        }

        private void DrawWireframe(SpriteCache sprite)
        {
            Debug.Assert(Event.current.type == EventType.Repaint);
            Debug.Assert(sprite != null);

            MeshPreviewCache meshPreview = sprite.GetMeshPreview();
            Debug.Assert(meshPreview != null);

            m_Material.mainTexture = null;
            m_Material.SetFloat("_Opacity", 0.35f);
            m_Material.SetFloat("_VertexColorBlend", 0f);
            m_Material.color = Color.white;

            GL.wireframe = true;
            DrawingUtility.DrawMesh(meshPreview.mesh, m_Material, sprite.GetLocalToWorldMatrixFromMode());
            GL.wireframe = false;
        }
    }
}
