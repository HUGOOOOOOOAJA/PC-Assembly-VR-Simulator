using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class RamPressDemonstration : MonoBehaviour
{
    [SerializeField] Transform[] m_Thumbs = System.Array.Empty<Transform>();
    [SerializeField] Transform[] m_Latches = System.Array.Empty<Transform>();
    [SerializeField] Transform m_Module;
    [SerializeField] GameObject m_Visuals;
    [SerializeField] TMP_Text m_StepLabel;
    Vector3[] m_ThumbHomes;
    Vector3 m_ModuleHome;
    float m_StartTime;

    void Awake()
    {
        m_ThumbHomes = new Vector3[m_Thumbs.Length];
        for (var index = 0; index < m_Thumbs.Length; index++)
            m_ThumbHomes[index] = m_Thumbs[index].localPosition;
        if (m_Module != null)
            m_ModuleHome = m_Module.localPosition;
    }

    void OnEnable()
    {
        m_StartTime = Time.unscaledTime;
        RenderPipelineManager.beginCameraRendering += FaceCamera;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= FaceCamera;
    }

    void FaceCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
            return;
        var direction = Vector3.ProjectOnPlane(transform.position - camera.transform.position, Vector3.up);
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    void Update()
    {
        if (m_Module == null || m_ThumbHomes == null)
            return;
        var phase = Mathf.Repeat(Time.unscaledTime - m_StartTime, 6f);
        var approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(phase / 1.2f));
        var press = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - 1.2f) / 0.9f));
        var lift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase - 3.5f) / 0.8f));
        m_Module.localPosition = m_ModuleHome - Vector3.up * (0.008f * press);
        for (var index = 0; index < m_Thumbs.Length; index++)
            m_Thumbs[index].localPosition = m_ThumbHomes[index] - Vector3.up * ((0.052f * approach + 0.008f * press) * (1f - lift));
        for (var index = 0; index < m_Latches.Length; index++)
            m_Latches[index].localRotation = Quaternion.Euler(0f, 0f, (index == 0 ? 55f : -55f) * (1f - press));
        if (m_Visuals != null)
            m_Visuals.SetActive(phase < 4.6f);
        if (m_StepLabel != null)
            m_StepLabel.text = phase < 1.2f ? "1. UM POLEGAR EM CADA PONTA" :
                phase < 2.1f ? "2. PRESSIONE RETO, OS DOIS JUNTOS" :
                phase < 4.6f ? "3. RAM ENCAIXADA, TRAVAS FECHADAS" : "OBSERVE O MOVIMENTO NOVAMENTE";
    }
}
