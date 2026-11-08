using System;
using System.Collections;
using System.Collections.Generic;
using DecoAR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DecoAR.M5
{
    /// Discuter avec le vendeur (Aj) : près du comptoir, un bouton « Discuter avec Aj » apparaît (ou toucher le vendeur).
    /// Une fenêtre de dialogue s'ouvre en bas : Aj répond (texte qui s'écrit, il parle et se tourne vers le joueur),
    /// réponses personnalisées (prénom, heure, panier) et choix qui mènent à de vraies actions :
    /// conseil produit à ajouter au panier, catalogue, paiement, adresse sur la carte.
    public class M5VendorChat : MonoBehaviour
    {
        const float NearDistance = 3.4f, LeaveDistance = 5f;

        M5CheckoutUI shop;
        VendorNPC vendor;
        RectTransform prompt, sheet, choices;
        CanvasGroup promptGroup, sheetGroup;
        TextMeshProUGUI line, nameLabel;
        bool open, typing;
        Coroutine typeRoutine;
        Vector2 pressPos; float pressTime; bool pressing;
        M5CheckoutUI.Product advice;

        struct Choice { public string label; public M5Icons.Kind icon; public Action action; }

        public void Init(M5CheckoutUI shop, RectTransform root)
        {
            this.shop = shop;
            vendor = VendorNPC.Instance;
            EnhancedTouchSupport.Enable();
            Build(root);
        }

        // ------------------------------------------------------------ boucle

        void Update()
        {
            if (vendor == null) vendor = VendorNPC.Instance;
            if (vendor == null || sheet == null) return;
            var cam = Camera.main;
            float dist = cam ? Flat(cam.transform.position - vendor.transform.position) : 99f;

            bool near = dist < NearDistance && shop.OpenPage == M5CheckoutUI.Page.Aucune && !shop.PaymentOpen;
            float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            promptGroup.alpha = Mathf.Lerp(promptGroup.alpha, near && !open ? 1f : 0f, k);
            promptGroup.interactable = promptGroup.blocksRaycasts = near && !open;
            // le bouton « Payer » occupe le bas de l'écran au comptoir : on se place au-dessus
            prompt.anchoredPosition = new Vector2(0, Mathf.Lerp(prompt.anchoredPosition.y, shop.PayButtonVisible ? 156f : 40f, k));

            sheetGroup.alpha = Mathf.Lerp(sheetGroup.alpha, open ? 1f : 0f, k);
            sheetGroup.interactable = sheetGroup.blocksRaycasts = open;
            sheet.anchoredPosition = new Vector2(0, Mathf.Lerp(sheet.anchoredPosition.y, open ? 24f : -60f, k));

            if (open && (dist > LeaveDistance || shop.OpenPage != M5CheckoutUI.Page.Aucune)) Close(false);
            if (!open && near) DetectVendorTap(cam);
        }

        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }

        /// Toucher le vendeur ouvre aussi la discussion.
        void DetectVendorTap(Camera cam)
        {
            Vector2 pos; bool down;
            if (ETouch.activeTouches.Count > 0) { pos = ETouch.activeTouches[0].screenPosition; down = true; }
            else if (Mouse.current != null) { pos = Mouse.current.position.ReadValue(); down = Mouse.current.leftButton.isPressed; }
            else return;
            if (down && !pressing) { pressing = true; pressPos = pos; pressTime = Time.unscaledTime; return; }
            if (down || !pressing) return;
            pressing = false;
            if ((pos - pressPos).magnitude > 24f || Time.unscaledTime - pressTime > 0.45f || OverUI(pos) || cam == null) return;
            if (Physics.Raycast(cam.ScreenPointToRay(pos), out var hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
                && hit.collider.transform.IsChildOf(vendor.transform))
                Open();
        }

        static bool OverUI(Vector2 p)
        {
            var es = EventSystem.current;
            if (!es) return false;
            var hits = new List<RaycastResult>();
            es.RaycastAll(new PointerEventData(es) { position = p }, hits);
            return hits.Count > 0;
        }

        // ------------------------------------------------------------ dialogue

        public void Open()
        {
            if (open || vendor == null) return;
            open = true;
            M5Haptics.Light();
            vendor.PlayGreet();
            var player = FindAnyObjectByType<M5PlayerController>();
            if (player) player.FocusOn(vendor.transform.position + Vector3.up * 1.55f);
            Say(Greeting(), MainChoices());
        }

        void Close(bool goodbye)
        {
            if (!open) return;
            open = false;
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typing = false;
            vendor.SetTalking(false);
            if (goodbye) vendor.ShowBubble(Hour() < 18 ? "Bonne journée !" : "Bonne soirée !", 2.5f);
        }

        string Greeting()
        {
            string name = shop.PlayerName == "Invité" ? "" : " " + shop.PlayerName;
            string hello = Hour() < 18 ? "Bonjour" : "Bonsoir";
            int n = shop.ItemCount;
            if (n > 0) return $"{hello}{name} ! Tu as {n} article{(n > 1 ? "s" : "")} pour {shop.FormatMoney(shop.Total)}. Je peux t'aider à finaliser ?";
            return $"{hello}{name} ! Je suis Aj, ton conseiller DecoAR. Qu'est-ce qui te ferait plaisir aujourd'hui ?";
        }

        static int Hour() => DateTime.Now.Hour;

        List<Choice> MainChoices()
        {
            var list = new List<Choice>
            {
                new Choice { label = "Un conseil déco ?", icon = M5Icons.Kind.Idee, action = Advice },
                new Choice { label = "Voir le catalogue", icon = M5Icons.Kind.Catalogue, action = Catalogue },
            };
            if (shop.ItemCount > 0) list.Add(new Choice { label = "Je veux payer", icon = M5Icons.Kind.Panier, action = Pay });
            list.Add(new Choice { label = "La livraison ?", icon = M5Icons.Kind.Position, action = Delivery });
            list.Add(new Choice { label = "Au revoir", icon = M5Icons.Kind.Fermer, action = () => Close(true) });
            return list;
        }

        void Advice()
        {
            var options = shop.catalog;
            if (options == null || options.Count == 0) { Say("Fais un tour dans le catalogue, tout y est en 3D !", MainChoices()); return; }
            var pick = options[UnityEngine.Random.Range(0, options.Count)];
            if (advice != null && options.Count > 1) while (pick == advice) pick = options[UnityEngine.Random.Range(0, options.Count)];
            advice = pick;
            string[] intros = { "Coup de cœur du moment :", "Je te conseille", "Ça irait très bien chez toi :", "Les clients adorent" };
            Say($"{intros[UnityEngine.Random.Range(0, intros.Length)]} <b>{pick.name}</b>, à {shop.FormatMoney(pick.price)}. Il réchauffe tout de suite une pièce !", new List<Choice>
            {
                new Choice { label = "Ajouter au panier", icon = M5Icons.Kind.Plus, action = () =>
                {
                    shop.AddProduct(pick);
                    Say($"C'est dans ton panier ! Autre chose ?", MainChoices());
                } },
                new Choice { label = "Autre idée", icon = M5Icons.Kind.Idee, action = Advice },
                new Choice { label = "Retour", icon = M5Icons.Kind.Retour, action = () => Say("Je t'écoute !", MainChoices()) },
            });
        }

        void Catalogue()
        {
            Say("Le catalogue complet est sur la télé, au fond de la boutique. Je t'y emmène ?", new List<Choice>
            {
                new Choice { label = "Ouvrir le catalogue", icon = M5Icons.Kind.Catalogue, action = () => { Close(false); shop.OpenCatalogue(); } },
                new Choice { label = "Retour", icon = M5Icons.Kind.Retour, action = () => Say("Je t'écoute !", MainChoices()) },
            });
        }

        void Pay()
        {
            Say($"Parfait ! Ton total est de <b>{shop.FormatMoney(shop.Total)}</b>. Vérifie ton adresse, puis touche Payer.", new List<Choice>
            {
                new Choice { label = "Voir mon panier", icon = M5Icons.Kind.Panier, action = () => { Close(false); shop.ShowPage(M5CheckoutUI.Page.Panier); } },
                new Choice { label = "Retour", icon = M5Icons.Kind.Retour, action = () => Say("Je t'écoute !", MainChoices()) },
            });
        }

        void Delivery()
        {
            string addr = shop.DeliveryAddress;
            string text = string.IsNullOrEmpty(addr)
                ? "On livre partout ! Choisis ton adresse sur la carte, ou utilise ta position."
                : $"Je livre à : <b>{addr}</b>. Tu veux la changer ?";
            Say(text, new List<Choice>
            {
                new Choice { label = "Choisir sur la carte", icon = M5Icons.Kind.Position, action = () => { Close(false); shop.OpenMap(); } },
                new Choice { label = "Retour", icon = M5Icons.Kind.Retour, action = () => Say("Je t'écoute !", MainChoices()) },
            });
        }

        void Say(string text, List<Choice> next)
        {
            if (typeRoutine != null) StopCoroutine(typeRoutine);
            typeRoutine = StartCoroutine(Type(text, next));
        }

        /// Le texte s'écrit lettre par lettre ; le vendeur parle pendant ce temps. Toucher la bulle termine la phrase.
        IEnumerator Type(string text, List<Choice> next)
        {
            typing = true;
            vendor.SetTalking(true);
            for (int i = choices.childCount - 1; i >= 0; i--) Destroy(choices.GetChild(i).gameObject);
            line.text = text;
            line.maxVisibleCharacters = 0;
            line.ForceMeshUpdate();
            int total = line.textInfo.characterCount;
            for (int i = 0; i <= total && typing; i++)
            {
                line.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(i % 7 == 0 ? 0.035f : 0.018f);
            }
            line.maxVisibleCharacters = 99999;
            typing = false;
            vendor.SetTalking(false);
            foreach (var c in next) ChoiceChip(c);
            typeRoutine = null;
        }

        void SkipTyping() { if (typing) typing = false; }

        // ------------------------------------------------------------ construction

        void Build(RectTransform root)
        {
            // Bouton « Discuter avec Aj »
            prompt = new GameObject("Discuter", typeof(RectTransform)).GetComponent<RectTransform>();
            prompt.SetParent(root, false);
            prompt.anchorMin = prompt.anchorMax = prompt.pivot = new Vector2(0.5f, 0);
            prompt.anchoredPosition = new Vector2(0, 40f);
            prompt.sizeDelta = new Vector2(0, 92);
            var bg = DecoUI.Glassy(prompt, DecoUI.Glass);
            var h = prompt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(26, 34, 0, 0); h.spacing = 14; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            prompt.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var bubbleIcon = DecoUI.Box("Pastille", prompt, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, DecoUI.Brass);
            bubbleIcon.GetComponent<Image>().sprite = DecoUI.Circle; bubbleIcon.GetComponent<Image>().type = Image.Type.Simple;
            var ble = bubbleIcon.GetComponent<LayoutElement>(); ble.preferredWidth = ble.preferredHeight = 58; ble.minWidth = 58;
            DecoUI.Icon(bubbleIcon, M5Icons.Kind.Profil, 32, DecoUI.Ink).GetComponent<LayoutElement>().ignoreLayout = true;
            var pt = DecoUI.Label(prompt, "<b>Discuter avec Aj</b>", 26, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            pt.textWrappingMode = TextWrappingModes.NoWrap; pt.rectTransform.offsetMin = Vector2.zero;
            var pb = prompt.gameObject.AddComponent<Button>(); pb.targetGraphic = bg; pb.onClick.AddListener(Open);
            promptGroup = prompt.gameObject.AddComponent<CanvasGroup>(); promptGroup.alpha = 0;

            // Fenêtre de dialogue
            sheet = DecoUI.Box("Dialogue vendeur", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -60f), new Vector2(1100, 0), Color.clear);
            sheet.pivot = new Vector2(0.5f, 0);
            var sh = DecoUI.Layer(sheet, "Ombre", DecoUI.Shadow, new Color(0, 0, 0, 0.45f), false);
            sh.rectTransform.offsetMin = new Vector2(-30, -34); sh.rectTransform.offsetMax = new Vector2(30, 20);
            var fond = DecoUI.Layer(sheet, "Fond", DecoUI.Rounded, DecoUI.GlassStrong, true);
            DecoUI.Layer(sheet, "Liseré", DecoUI.RingSprite, new Color(1, 1, 1, 0.12f), false);
            fond.gameObject.AddComponent<Button>().onClick.AddListener(SkipTyping);
            var v = DecoUI.Column(sheet, 14, 26);
            v.padding = new RectOffset(28, 28, 22, 22);
            sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sheetGroup = sheet.gameObject.AddComponent<CanvasGroup>(); sheetGroup.alpha = 0;

            var head = DecoUI.Row(sheet, 64, 14, Color.clear, 0, 0);
            var avatar = DecoUI.Box("Avatar", head, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, DecoUI.Brass);
            avatar.GetComponent<Image>().sprite = DecoUI.Circle; avatar.GetComponent<Image>().type = Image.Type.Simple;
            var ale = avatar.GetComponent<LayoutElement>(); ale.preferredWidth = ale.preferredHeight = 60; ale.minWidth = 60;
            DecoUI.Icon(avatar, M5Icons.Kind.Profil, 34, DecoUI.Ink).GetComponent<LayoutElement>().ignoreLayout = true;
            nameLabel = DecoUI.Label(head, "<b>Aj</b>  <size=80%><color=#DDCDB5>Conseiller DecoAR</color></size>", 26, DecoUI.Cream, TextAlignmentOptions.MidlineLeft);
            nameLabel.GetComponent<LayoutElement>().flexibleWidth = 1; nameLabel.rectTransform.offsetMin = Vector2.zero;
            DecoUI.IconButton(head, M5Icons.Kind.Fermer, 56, DecoUI.Tile, DecoUI.Cream, () => Close(true));

            line = DecoUI.Label(sheet, "", 28, DecoUI.Cream, TextAlignmentOptions.TopLeft);
            line.textWrappingMode = TextWrappingModes.Normal;
            line.rectTransform.offsetMin = Vector2.zero;
            line.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            line.GetComponent<LayoutElement>().minHeight = 80;

            choices = DecoUI.Row(sheet, 70, 12, Color.clear, 0, 0);
            choices.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        }

        void ChoiceChip(Choice c)
        {
            var chip = DecoUI.Pill(choices, c.label, c.label == "Ajouter au panier" ? DecoUI.Sage : DecoUI.Tile,
                                   c.label == "Ajouter au panier" ? Color.white : DecoUI.Cream, () => c.action(), 66, 22, c.icon);
            chip.GetComponent<LayoutElement>().preferredHeight = 66;
            // apparition douce
            var g = chip.gameObject.AddComponent<CanvasGroup>(); g.alpha = 0;
            StartCoroutine(FadeChip(g, choices.childCount * 0.05f));
        }

        static IEnumerator FadeChip(CanvasGroup g, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            for (float t = 0; t < 0.2f && g; t += Time.unscaledDeltaTime) { g.alpha = t / 0.2f; yield return null; }
            if (g) g.alpha = 1f;
        }
    }
}
