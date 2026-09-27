using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Opt-in alternate action. Unity Button retains sole ownership of left clicks.</summary>
public sealed class SecondaryClick : MonoBehaviour, IPointerClickHandler
{
    public Action? Action;
    private Button? button;
    public static void Bind(Button button, Action action)
    {
        var handler = button.GetComponent<SecondaryClick>() ?? button.gameObject.AddComponent<SecondaryClick>();
        handler.button = button; handler.Action = action;
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Right) return;
        data.Use(); CrewSim.bJustClickedInput = true;
        if (button != null && button.IsActive() && button.IsInteractable()) Action?.Invoke();
    }
}
