using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class RamAssemblyHint : MonoBehaviour
{
    [SerializeField] RamSlotLatches[] m_Slots = System.Array.Empty<RamSlotLatches>();
    [SerializeField] XRGrabInteractable[] m_Modules = System.Array.Empty<XRGrabInteractable>();
    [SerializeField] TMP_Text m_Label;

    void OnEnable()
    {
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
        var direction = transform.position - camera.transform.position;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    void LateUpdate()
    {
        if (m_Label == null)
            return;
        var allInstalled = m_Slots.Length > 0;
        foreach (var slot in m_Slots)
            allInstalled &= slot != null && slot.Socket != null && slot.Socket.hasSelection;
        if (allInstalled)
        {
            m_Label.text = "<b>MEMÓRIAS RAM INSTALADAS</b>\nOs quatro slots estão ocupados.";
            return;
        }

        RamSlotLatches focusedSlot = null;
        foreach (var slot in m_Slots)
        {
            if (slot != null && slot.IsFocused)
            {
                focusedSlot = slot;
                break;
            }
        }
        if (focusedSlot == null)
        {
            var closestDistance = 0.2f * 0.2f;
            foreach (var module in m_Modules)
            {
                if (module == null || !module.isSelected || module.firstInteractorSelecting is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
                    continue;
                foreach (var slot in m_Slots)
                {
                    if (slot == null || slot.Socket == null)
                        continue;
                    var distance = (module.transform.position - slot.Socket.attachTransform.position).sqrMagnitude;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        focusedSlot = slot;
                    }
                }
            }
        }

        if (focusedSlot == null)
            m_Label.text = "<b>PREPARAR A MEMÓRIA RAM</b>\nAbra as duas travas do slot escolhido.\nFaça pinça nas travas e mova-as para fora.";
        else if (focusedSlot.Socket.hasSelection)
            m_Label.text = $"<b>{focusedSlot.name.Replace("RAM_SLOT_", "SLOT ")}</b>\nMemória RAM instalada.";
        else if (!focusedSlot.IsOpen)
            m_Label.text = $"<b>{focusedSlot.name.Replace("RAM_SLOT_", "SLOT ")}</b>\nAbra as DUAS travas para inserir a RAM.\nFaça pinça e mova cada trava para fora.";
        else
            m_Label.text = $"<b>{focusedSlot.name.Replace("RAM_SLOT_", "SLOT ")}</b>\nTravas abertas.\nAlinhe os contatos dourados e coloque a RAM.";
    }
}
