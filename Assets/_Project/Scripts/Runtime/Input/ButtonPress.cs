using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace PerfectParticipant
{
    // On ConsentButton next to an XRSimpleInteractable + XRPokeFilter. Poke or controller select = press. Cubes never select.
    public class ButtonPress : MonoBehaviour
    {
        public static event Action Pressed;
        XRBaseInteractable xri;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Pressed = null;

        void OnEnable() { xri = GetComponent<XRBaseInteractable>(); if (xri) xri.selectEntered.AddListener(OnSel); }
        void OnDisable() { if (xri) xri.selectEntered.RemoveListener(OnSel); }
        void OnSel(SelectEnterEventArgs _) => Press();

        public void Press() => Pressed?.Invoke(); // DesktopRig F key
    }
}
