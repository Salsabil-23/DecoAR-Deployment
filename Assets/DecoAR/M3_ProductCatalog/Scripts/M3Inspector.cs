using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DecoAR.M3
{
    // Selection et inspection 3D d'un produit :
    // F3.2 le produit sort de l'etagere et se place face a la camera,
    // F3.3 rotation 360° au doigt et zoom par pincement (souris : glisser + molette dans l'editeur).
    public class M3Inspector : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform inspectPoint;          // devant la camera, axe +Z tourne vers elle
        [SerializeField] float inspectSize = 0.45f;       // cote max du produit inspecte (m)
        [SerializeField] float moveDuration = 0.45f;
        [SerializeField] float rotateSpeed = 0.35f;       // degres par pixel
        [SerializeField] float minZoom = 0.6f;
        [SerializeField] float maxZoom = 2.2f;
        [SerializeField] float tapMaxMove = 18f;          // pixels

        public M3ShelfItem Current { get; private set; }
        public float InspectSize { get => inspectSize; set => inspectSize = value; }
        public bool IsBusy => moving != null;

        public event Action<M3ShelfItem> ItemTapped;      // tap sur un produit de l'etagere
        public event Action<M3ShelfItem> Inspected;
        public event Action Returned;

        Coroutine moving;
        float zoom = 1f;
        float baseScale = 1f;
        float yaw, pitch;

        Vector2 pressPos, lastPos;
        bool pressing, pressOverUI, dragged;
        float lastPinchDistance;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        // ------------------------------------------------------------ selection

        public void Inspect(M3ShelfItem item)
        {
            if (item == null || item == Current || IsBusy) return;
            if (Current != null)
            {
                var previous = Current;
                Current = null;
                StartCoroutine(ReturnRoutine(previous, null));
            }

            Current = item;
            item.StopBobbing();
            zoom = 1f;
            yaw = 0f;
            pitch = 0f;

            // Echelle pour que le produit occupe l'ecran sans le deborder
            float biggest = Mathf.Max(item.LocalSize.x, item.LocalSize.y, item.LocalSize.z);
            baseScale = biggest > 0.0001f ? inspectSize / biggest : 1f;

            item.transform.SetParent(inspectPoint, true);
            moving = StartCoroutine(Tracked(MoveRoutine(item.transform, PivotFor(Quaternion.identity, baseScale), Quaternion.identity, Vector3.one * baseScale, () =>
            {
                Inspected?.Invoke(item);
            })));
        }

        // Repose le produit inspecte sur l'etagere (bouton "Reposer" ou secousse F3.8)
        public void ReturnCurrent()
        {
            if (Current == null) return;
            if (moving != null) { StopCoroutine(moving); moving = null; }
            var item = Current;
            Current = null;
            moving = StartCoroutine(Tracked(ReturnRoutine(item, () => Returned?.Invoke())));
        }

        // Avant de vider l'etagere (changement de zone) : plus aucune animation en cours
        public void Release()
        {
            StopAllCoroutines();
            moving = null;
            Current = null;
        }

        public void ApplyVariant(ProductVariant variant)
        {
            if (Current != null) M3VariantApplier.Apply(Current.Model, variant);
        }

        IEnumerator ReturnRoutine(M3ShelfItem item, Action done)
        {
            item.transform.SetParent(item.HomeParent, true);
            yield return MoveRoutine(item.transform, item.HomeLocalPosition, item.HomeLocalRotation, item.HomeLocalScale, null);
            done?.Invoke();
        }

        IEnumerator MoveRoutine(Transform t, Vector3 localPos, Quaternion localRot, Vector3 localScale, Action done)
        {
            Vector3 p0 = t.localPosition;
            Quaternion r0 = t.localRotation;
            Vector3 s0 = t.localScale;
            for (float time = 0f; time < moveDuration; time += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, time / moveDuration);
                // petit arc vers le haut a la sortie de l'etagere
                t.localPosition = Vector3.Lerp(p0, localPos, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.05f;
                t.localRotation = Quaternion.Slerp(r0, localRot, k);
                t.localScale = Vector3.Lerp(s0, localScale, k);
                yield return null;
            }
            t.localPosition = localPos;
            t.localRotation = localRot;
            t.localScale = localScale;
            done?.Invoke();
        }

        // Une seule animation "bloquante" a la fois (IsBusy)
        IEnumerator Tracked(IEnumerator routine)
        {
            yield return routine;
            moving = null;
        }

        // Le pivot des produits est en bas : decalage pour que le centre du modele reste sur le point d'inspection
        Vector3 PivotFor(Quaternion rotation, float scale)
            => Current == null ? Vector3.zero : -(rotation * (Current.LocalCenter * scale));

        // ------------------------------------------------------------ entrees

        void Update()
        {
            if (Touch.activeTouches.Count >= 2) { HandlePinch(); return; }
            lastPinchDistance = 0f;

            if (Touch.activeTouches.Count == 1) HandlePointer(Touch.activeTouches[0].screenPosition, true);
            else if (Mouse.current != null && Touchscreen.current == null) HandlePointer(Mouse.current.position.ReadValue(), Mouse.current.leftButton.isPressed);
            else HandlePointer(lastPos, false);

            if (Current != null && !IsBusy && Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) SetZoom(zoom * (1f + Mathf.Sign(scroll) * 0.1f));
            }
        }

        void HandlePointer(Vector2 pos, bool down)
        {
            if (down && !pressing)
            {
                pressing = true;
                dragged = false;
                pressOverUI = IsOverUI(pos);
                pressPos = lastPos = pos;
                return;
            }

            if (down && pressing)
            {
                if (!pressOverUI && (pos - pressPos).magnitude > tapMaxMove) dragged = true;
                if (dragged && Current != null && !IsBusy)
                {
                    Vector2 delta = pos - lastPos;
                    yaw -= delta.x * rotateSpeed;
                    pitch = Mathf.Clamp(pitch + delta.y * rotateSpeed, -60f, 60f);
                    ApplyRotation();
                }
                lastPos = pos;
                return;
            }

            if (!down && pressing)
            {
                pressing = false;
                if (!pressOverUI && !dragged) Tap(pos);
            }
        }

        void HandlePinch()
        {
            pressing = false;
            if (Current == null || IsBusy) return;
            var a = Touch.activeTouches[0].screenPosition;
            var b = Touch.activeTouches[1].screenPosition;
            float d = Vector2.Distance(a, b);
            if (lastPinchDistance > 0f && d > 1f) SetZoom(zoom * d / lastPinchDistance);
            lastPinchDistance = d;
        }

        void Tap(Vector2 pos)
        {
            if (viewCamera == null || IsBusy) return;
            var ray = viewCamera.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out var hit, 20f) && hit.collider.TryGetComponent<M3ShelfItem>(out var item) && item != Current)
                ItemTapped?.Invoke(item);
        }

        void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, minZoom, maxZoom);
            if (Current == null) return;
            Current.transform.localScale = Vector3.one * baseScale * zoom;
            ApplyRotation();
        }

        void ApplyRotation()
        {
            if (Current == null) return;
            var t = Current.transform;
            t.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            t.localPosition = PivotFor(t.localRotation, t.localScale.x);
        }

        bool IsOverUI(Vector2 pos)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pos }, uiHits);
            return uiHits.Count > 0;
        }
    }
}
