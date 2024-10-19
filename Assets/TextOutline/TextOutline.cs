using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GMModule
{
    [RequireComponent(typeof(Text))]
    public class TextOutline : BaseMeshEffect
    {
        //static string sOutlineShader = "Tool/MyOutline";
        [HideInInspector]
        public Material material;
        Text mText;
        Text mLangText;
        bool mFindRenderCanvas;
        bool bSetPreviewCanvas = false;
        int iMatHash = 0;
        List<UIVertex> lVetexList = new List<UIVertex>();
        List<UIVertex> lShadowVerts = new List<UIVertex>();
        List<UIVertex> mergedVerts = new List<UIVertex>();

        #region 描边
        [Header("描边颜色")]
        [SerializeField]
        private Color outlineColor = Color.white;
        public Color OutlineColor
        {
            get
            {
                return outlineColor;
            }
            set
            {
                outlineColor = value;
                if (base.graphic != null)
                    _Refresh();
            }
        }

        [Header("描边宽度"), Range(0, 8)]
        [SerializeField]
        private float outlineWidth = 0;
        public float OutlineWidth
        {
            get
            {
                return outlineWidth;
            }
            set
            {
                outlineWidth = value;
                if (base.graphic != null)
                    _Refresh();
            }
        }
        #endregion

        #region 渐变
        [Header("【使用渐变】")]
        [SerializeField]
        private bool bUseTextGradient = false;

        public bool UseTextGradient
        {
            get
            {
                return bUseTextGradient;
            }
            set
            {
                bUseTextGradient = value;
                if (base.graphic != null)
                    _Refresh();
            }
        }
        [Header("渐变类型")]
        [SerializeField]
        GradientType GraType = GradientType.Vertical;
        [Header("渐变颜色")]
        [SerializeField]
        private Gradient textGradient;
        public Gradient TextGradient
        {
            get
            {
                return textGradient;
            }
            set
            {
                textGradient = value;
                if (base.graphic != null)
                    _Refresh();
            }
        }

        #endregion

        #region 曲线
        [Header("【使用曲线】")]
        public bool UseCurve;

        [SerializeField]
        AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 200);

        [SerializeField]
        float curveMultiplier = 0.01f;

        RectTransform rectTransform;



        #endregion

        #region 弧形
        [Header("【使用弧形】")]
        public bool UseArc;
        [Header("弧度")]
        [SerializeField]
        int radius = 500;
        [Header("间隔")]
        [SerializeField]
        float space = 1f;
        #endregion

        #region 背面剔除
        [Header("背面剔除")]
        [SerializeField]
        private UnityEngine.Rendering.CullMode mCullMode = UnityEngine.Rendering.CullMode.Off;
        public UnityEngine.Rendering.CullMode CullMode
        {
            get
            {
                return mCullMode;
            }
            set
            {
                mCullMode = value;
                if (base.graphic != null)
                    _Refresh();
            }
        }
        #endregion

        bool isDirty = false;
        Font dirtyFont = null;

        protected override void Awake()
        {
            base.Awake();
            SetMat();
            ShaderRefresh();

            mText = GetComponent<Text>();
            mLangText = GetComponent<Text>();
            rectTransform = GetComponent<RectTransform>();
            //mat = mText.font.material;
            Font.textureRebuilt += OnFontTextureRebuilt;//字体贴图膨胀
        }


        void OnFontTextureRebuilt(Font font)
        {
            isDirty = true;
            dirtyFont = font;
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            OnRectTransformDimensionsChange();
        }

        //当RectTransform尺寸发生变化时
        protected override void OnRectTransformDimensionsChange()
        {
            if (rectTransform != null)
            {
                Keyframe temp = curve[curve.length - 1];
                temp.time = rectTransform.rect.width;
                curve.MoveKey(curve.length - 1, temp);
            }
        }

        void ShaderRefresh()
        {
            if (graphic != null && graphic.material != null)
            {
                _Refresh();
            }
        }

        void SetMat()
        {
            if (base.graphic.material.GetHashCode() != iMatHash)
            {
                if (material == null)
                {
                    material = Resources.Load<Material>("Tool_MyOutline");
                }
                base.graphic.material = material;
                iMatHash = base.graphic.material.GetHashCode();
            }
        }

        void SetShaderParams()
        {
            if (base.graphic.material != null)
            {
                //base.graphic.material.SetColor("_OutlineColor", OutlineColor);
                //base.graphic.material.SetFloat("_OutlineWidth", OutlineWidth);
                //base.graphic.material.SetVector("_OutlineOffset", ShadowOutlineOffset);

                //base.graphic.material.SetColor("_ShadowOutlineColor", Color.white);
                //base.graphic.material.SetFloat("_ShadowOutlineWidth", ShadowOutlineWidth);
                base.graphic.material.SetInt("_Cull", (int)mCullMode);
            }

        }

        void SetShaderChannels()
        {
            if (graphic.canvas)
            {
                mFindRenderCanvas = true;
                AdditionalCanvasShaderChannels v1 = graphic.canvas.additionalShaderChannels;
                AdditionalCanvasShaderChannels v2 = AdditionalCanvasShaderChannels.TexCoord1;
                if ((v1 & v2) != v2)
                {
                    graphic.canvas.additionalShaderChannels |= v2;
                }
                v2 = AdditionalCanvasShaderChannels.TexCoord2;
                if ((v1 & v2) != v2)
                {
                    graphic.canvas.additionalShaderChannels |= v2;
                }
                v2 = AdditionalCanvasShaderChannels.TexCoord3;
                if ((v1 & v2) != v2)
                {
                    graphic.canvas.additionalShaderChannels |= v2;
                }

            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            lVetexList.Clear();
            vh.GetUIVertexStream(lVetexList);
            _ProcessVertices(lVetexList, OutlineWidth);
            //曲线 根据曲线图修改顶点的位置
            if (UseCurve)
            {
                for (int index = 0; index < lVetexList.Count; index++)
                {
                    UIVertex uiVertex = lVetexList[index];
                    uiVertex.position.y += curve.Evaluate(rectTransform.rect.width * rectTransform.pivot.x + uiVertex.position.x) * curveMultiplier;
                    lVetexList[index] = uiVertex;
                }
            }
            else if (UseArc)
            {
                ArcText(vh, lVetexList);
            }

            //渐变
            if (bUseTextGradient)
            {
                ApplyGradient(lVetexList, textGradient);
            }

            vh.Clear();
            mergedVerts.Clear();
            for (int i = 0; i < lShadowVerts.Count; i++)
            {
                mergedVerts.Add(lShadowVerts[i]);
            }
            for (int j = 0; j < lVetexList.Count; j++)
            {
                mergedVerts.Add(lVetexList[j]);
            }
            vh.AddUIVertexTriangleStream(mergedVerts);
            lShadowVerts.Clear();
            lVetexList.Clear();
        }

        private void ApplyGradient(List<UIVertex> _verts, Gradient _gradient)
        {
            if (_verts.Count == 0) return;
            float topY = _verts[0].position.y;
            float bottomY = _verts[0].position.y;
            float leftX = _verts[0].position.x;
            float rightX = _verts[0].position.x;
            //拿到4个端点
            for (var i = 0; i < _verts.Count; i++)
            {
                float y = _verts[i].position.y;
                if (y > topY)
                    topY = y;
                else if (y < bottomY)
                    bottomY = y;

                float x = _verts[i].position.x;
                if (x > rightX)
                    rightX = x;
                else if (x < leftX)
                    leftX = x;
            }
            float height = topY - bottomY;
            float width = rightX - leftX;
            if (height == 0 || width == 0)
                return;
            Color32 color;
            for (int i = 0; i < _verts.Count; i++)
            {
                UIVertex vt = _verts[i];
                if (GraType == GradientType.Vertical)
                    color = _gradient.Evaluate((topY - vt.position.y) / height);
                else
                    color = _gradient.Evaluate(1 - ((rightX - vt.position.x) / width));
                color.a = (byte)((color.a * vt.color.a) / 255);
                vt.color = color;
                _verts[i] = vt;
            }
        }


        void ArcText(VertexHelper vh, List<UIVertex> vetexList)
        {
            for (int i = 0; i < vetexList.Count / 6; i++)
            {
                //3和5是重合的 不需要再计算
                UIVertex leftT = vetexList[i * 6];
                UIVertex rightT = vetexList[i * 6 + 1];
                UIVertex rightB = vetexList[i * 6 + 2];
                UIVertex leftB = vetexList[i * 6 + 4];

                Vector3 center = Vector3.Lerp(leftT.position, rightB.position, 0.5f);
                Matrix4x4 move = Matrix4x4.TRS(center * -1, Quaternion.identity, Vector3.one);
                float rad = Mathf.PI / 2 - center.x * space / radius;
                Vector3 pos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * radius;
                Quaternion rotation = Quaternion.Euler(0, 0, rad * 180 / Mathf.PI - 90);
                Matrix4x4 rotate = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
                Matrix4x4 place = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                Matrix4x4 matrix = place * rotate * move;

                leftT.position = matrix.MultiplyPoint(leftT.position);
                rightT.position = matrix.MultiplyPoint(rightT.position);
                rightB.position = matrix.MultiplyPoint(rightB.position);
                leftB.position = matrix.MultiplyPoint(leftB.position);
                leftT.position.y = leftT.position.y - radius + center.y;
                rightT.position.y = rightT.position.y - radius + center.y;
                rightB.position.y = rightB.position.y - radius + center.y;
                leftB.position.y = leftB.position.y - radius + center.y;

                vetexList[i * 6] = leftT;
                vetexList[i * 6 + 1] = rightT;
                vetexList[i * 6 + 2] = rightB;
                vetexList[i * 6 + 3] = leftT;
                vetexList[i * 6 + 4] = rightB;
                vetexList[i * 6 + 5] = leftB;
            }
        }

        // 添加描边后，为防止描边被网格边框裁切，需要将顶点外扩，同时保持UV不变
        private void _ProcessVertices(List<UIVertex> lVerts, float outlineWidth)
        {
            for (int i = 0, count = lVerts.Count - 3; i <= count; i += 3)
            {
                UIVertex v1 = lVerts[i];
                UIVertex v2 = lVerts[i + 1];
                UIVertex v3 = lVerts[i + 2];

               
                // 计算原顶点坐标中心点
                //
                float minX = _Min(v1.position.x, v2.position.x, v3.position.x);
                float minY = _Min(v1.position.y, v2.position.y, v3.position.y);
                float maxX = _Max(v1.position.x, v2.position.x, v3.position.x);
                float maxY = _Max(v1.position.y, v2.position.y, v3.position.y);
                Vector2 posCenter = new Vector2(minX + maxX, minY + maxY) * 0.5f;
                // 计算原始顶点坐标和UV的方向
                //
                Vector2 triX, triY, uvX, uvY;
                Vector2 pos1 = v1.position;
                Vector2 pos2 = v2.position;
                Vector2 pos3 = v3.position;
                if (Mathf.Abs(Vector2.Dot((pos2 - pos1).normalized, Vector2.right))
                    > Mathf.Abs(Vector2.Dot((pos3 - pos2).normalized, Vector2.right)))
                {
                    triX = pos2 - pos1;
                    triY = pos3 - pos2;
                    uvX = v2.uv0 - v1.uv0;
                    uvY = v3.uv0 - v2.uv0;
                }
                else
                {
                    triX = pos3 - pos2;
                    triY = pos2 - pos1;
                    uvX = v3.uv0 - v2.uv0;
                    uvY = v2.uv0 - v1.uv0;
                }
                // 计算原始UV框
                Vector2 uvMin = _Min(v1.uv0, v2.uv0, v3.uv0);
                Vector2 uvMax = _Max(v1.uv0, v2.uv0, v3.uv0);

                // 为每个顶点设置新的Position和UV，并传入原始UV框
                v1 = _SetNewPosAndUV(v1, outlineWidth, posCenter, triX, triY, uvX, uvY, uvMin, uvMax, Vector2.zero);
                v2 = _SetNewPosAndUV(v2, outlineWidth, posCenter, triX, triY, uvX, uvY, uvMin, uvMax, Vector2.zero);
                v3 = _SetNewPosAndUV(v3, outlineWidth, posCenter, triX, triY, uvX, uvY, uvMin, uvMax, Vector2.zero);

                // 应用设置后的UIVertex
                //
                lVerts[i] = v1;
                lVerts[i + 1] = v2;
                lVerts[i + 2] = v3;
            }
        }


        UIVertex _SetNewPosAndUV(UIVertex pVertex, float pOutLineWidth,
           Vector2 pPosCenter,
           Vector2 pTriangleX, Vector2 pTriangleY,
           Vector2 pUVX, Vector2 pUVY,
           Vector2 pUVOriginMin, Vector2 pUVOriginMax, Vector2 offset)
        {
            // Position
            Vector3 pos = pVertex.position;
            float posXOffset = pos.x > pPosCenter.x ? pOutLineWidth : -pOutLineWidth;
            float posYOffset = pos.y > pPosCenter.y ? pOutLineWidth : -pOutLineWidth;
            pos.x += posXOffset;
            pos.y += posYOffset;
            pVertex.position = pos;
            // UV
            Vector2 uv = pVertex.uv0;
            Vector2 uvOffsetX = pUVX / pTriangleX.magnitude * posXOffset * (Vector2.Dot(pTriangleX, Vector2.right) > 0 ? 1 : -1);
            Vector2 uvOffsetY = pUVY / pTriangleY.magnitude * posYOffset * (Vector2.Dot(pTriangleY, Vector2.up) > 0 ? 1 : -1);
            uv.x += (uvOffsetX.x + uvOffsetY.x);
            uv.y += (uvOffsetX.y + uvOffsetY.y);
            pVertex.uv0 = uv;

            pVertex.uv1 = pUVOriginMin;     //uv1 uv2 可用  tangent  normal 在缩放情况 会有问题
            pVertex.uv2 = pUVOriginMax;

            pVertex.uv3 = outlineColor;
            pVertex.uv3.w = outlineWidth;

            return pVertex;
        }

        void _Refresh()
        {
            SetShaderParams();
            base.graphic.SetVerticesDirty();//重新绘制
        }


        //有时候Awake的时候没有找到canvas  这个时候需要一直刷新一下 直到找到
        private void Update()
        {
            if (!mFindRenderCanvas)
            {
                SetShaderChannels();

            }
        }

        private void LateUpdate()
        {
            if (isDirty)
            {
                isDirty = false;
                if (mLangText != null && mLangText.font == dirtyFont)
                {
                    mLangText.FontTextureChanged();
                }
                if (mText != null && mText.font == dirtyFont)
                {
                    mText.FontTextureChanged();
                }
                dirtyFont = null;
            }
        }

        public void SetColor(Color outline, Color grad1, Color grad2)
        {
            textGradient.colorKeys = new GradientColorKey[] { new GradientColorKey(grad1, 0), new GradientColorKey(grad2, 1) };
            OutlineColor = outline;
        }

        static float _Min(float pA, float pB, float pC)
        {
            return Mathf.Min(Mathf.Min(pA, pB), pC);
        }


        static float _Max(float pA, float pB, float pC)
        {
            return Mathf.Max(Mathf.Max(pA, pB), pC);
        }


        static Vector2 _Min(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(_Min(pA.x, pB.x, pC.x), _Min(pA.y, pB.y, pC.y));
        }


        static Vector2 _Max(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(_Max(pA.x, pB.x, pC.x), _Max(pA.y, pB.y, pC.y));
        }


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!bSetPreviewCanvas && Application.isEditor && gameObject.activeInHierarchy)
            {
                Canvas can = GetComponentInParent<Canvas>();
                if (can != null)
                {
                    if ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0 || (can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord2) == 0)
                    {
                        if (can.name.Contains("(Environment)"))
                        {
                            // 处于Prefab预览场景中(需要打开TexCoord1,2,3通道，否则Scene场景上会有显示问题(因为我们有用到uv1,uv2.uv3))
                            can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
                        }
                        else
                        {
                            // 不是处于Prefab预览场景中，但父级Canvas的TexCoord1和TexCoord2通道没打开
                            SetShaderChannels();
                        }
                    }
                    bSetPreviewCanvas = true;
                }
            }

            SetMat();
            ShaderRefresh();


            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (UseCurve)
            {
                if (curve[0].time != 0)
                {
                    Keyframe tmpRect = curve[0];
                    tmpRect.time = 0;
                    curve.MoveKey(0, tmpRect);
                }

                if (curve[curve.length - 1].time != rectTransform.rect.width)
                    OnRectTransformDimensionsChange();
            }
        }
#endif

        protected override void OnDestroy()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
            base.OnDestroy();
            base.graphic.material = null;

        }

    }

    public enum GradientType
    {
        Horizontal,
        Vertical,
    }


    //[CustomEditor(typeof(MyOutline))]
    //public class OutlineEditor : Editor
    //{
    //    SerializedProperty mUseShadow;
    //    SerializedProperty mUseGradient;
    //    SerializedProperty mUseCurve;
    //    SerializedProperty mUseArc;

    //    void OnEnable()
    //    {
    //        mUseShadow = serializedObject.FindProperty("BUseShadow");
    //        mUseGradient = serializedObject.FindProperty("TextGradient");
    //        mUseCurve = serializedObject.FindProperty("UseCurve");
    //        mUseArc = serializedObject.FindProperty("UseArc");
    //    }

    //    //public override void OnInspectorGUI()
    //    //{
    //    //    //serializedObject.Update();
    //    //    //EditorGUILayout.PropertyField(mUseShadow);
    //    //    //if (mVarType.boolValue)
    //    //    //    EditorGUILayout.PropertyField(mBool);
    //    //    //else
    //    //    //    EditorGUILayout.PropertyField(mInt);
    //    //    //serializedObject.ApplyModifiedProperties();

    //    //    //SerializedProperty prpo = serializedObject.GetIterator();//获得第一个序列化属性
    //    //    //                                                         //while(prpo.Next(true))//遍历prpo会改
    //    //    //                                                         //{
    //    //    //                                                         //    ZStrTool.Log(prpo.propertyType, prpo.name);
    //    //    //                                                         //}
    //    //}

    //}

}