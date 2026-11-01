using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static DecoAR.Auth.EditorTools.AuthUiKit;

namespace DecoAR.Auth.EditorTools
{
    // Menu DecoAR > Auth : genere les reglages, la couche de verrouillage (prefab),
    // la scene Auth (inscription, verification, selfie, connexion, mot de passe oublie) et la scene Profil
    public static class AuthSceneBuilder
    {
        const string SceneFolder = Root + "/Scenes";
        const string ResourcesFolder = Root + "/Resources";
        const string AuthScenePath = SceneFolder + "/Auth.unity";
        const string ProfileScenePath = SceneFolder + "/Profile.unity";
        const string SettingsPath = ResourcesFolder + "/DecoAR_AuthSettings.asset";
        const string OverlayPath = ResourcesFolder + "/DecoAR_AuthOverlay.prefab";

        [MenuItem("DecoAR/Auth/Tout generer")]
        public static void BuildAll()
        {
            BuildSettingsAndOverlay();
            BuildProfileScene();
            BuildAuthScene(); // en dernier : reste ouverte
            Debug.Log("[Auth] Generation terminee");
        }

        [MenuItem("DecoAR/Auth/Verrouiller maintenant (test, en Play)")]
        static void LockNow() => AuthOverlay.Instance?.Lock();

        [MenuItem("DecoAR/Auth/Verrouiller maintenant (test, en Play)", true)]
        static bool LockNowValidate() => Application.isPlaying && AuthOverlay.Instance != null;

        [MenuItem("DecoAR/Auth/Reglages (adresse de l'API)")]
        static void SelectSettings()
        {
            BuildSettingsAndOverlay();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<AuthSettings>(SettingsPath);
        }

        // ---------------------------------------------------------------- reglages + prefab

        static void BuildSettingsAndOverlay()
        {
            LoadSprites();
            EnsureFolder(ResourcesFolder);
            if (AssetDatabase.LoadAssetAtPath<AuthSettings>(SettingsPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AuthSettings>(), SettingsPath);

            var root = NewCanvas("DecoAR_AuthOverlay", 500);
            var overlay = root.AddComponent<AuthOverlay>();
            var rt = root.transform;

            // Bouton profil rond, en haut a gauche (le HUD M1 occupe le haut a droite)
            var bubble = Rect("Bouton profil", rt, new Vector2(0, 1), new Vector2(100, -90), new Vector2(120, 120));
            var hit = Img(bubble, CircleSprite, new Color(0, 0, 0, 0.25f), 1f);
            var photo = RoundPhoto(bubble, 110, out var initials);
            var button = bubble.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            Tint(button);
            Set(overlay, "profileBubble", bubble.gameObject);
            Set(overlay, "profileButton", button);
            Set(overlay, "profilePhoto", photo);
            Set(overlay, "profileInitials", initials);

            // Ecran de verrouillage
            var lockPanel = Stretch("Verrouillage", rt);
            Background(lockPanel).GetComponent<Image>().raycastTarget = true; // bloque l'app derriere
            var stack = Rect("Contenu", lockPanel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 0));
            var v = stack.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            stack.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Logo(Elem("Logo", stack, 110), 100);
            Label(Elem("Titre", stack, 76), "DecoAR est verrouillé", 54, Cream, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            var subtitle = Label(Elem("Sous-titre", stack, 80), "", 30, Gold, TextAlignmentOptions.Center);
            // Camera : le visage doit correspondre au compte connecte (OpenCV cote serveur)
            var cameraGroup = Stack("Camera", stack, 12);
            var lockChallenge = ChallengeBlock(cameraGroup, "<color=#F3E9DD>Utiliser mon mot de passe</color>", 300, Cream);
            var password = PrimaryButton(stack, "Me reconnecter");
            var status = Label(Elem("Message", stack, 64), "", 28, Hex("F2A38A"), TextAlignmentOptions.Center);
            Set(overlay, "lockPanel", lockPanel.gameObject);
            Set(overlay, "lockSubtitle", subtitle);
            Set(overlay, "lockCameraGroup", cameraGroup.gameObject);
            Set(overlay, "lockChallenge", lockChallenge);
            Set(overlay, "passwordButton", password);
            Set(overlay, "lockStatus", status);
            cameraGroup.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, OverlayPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- scene Auth

        [MenuItem("DecoAR/Auth/Creer la scene inscription + connexion")]
        public static void BuildAuthScene()
        {
            LoadSprites();
            EnsureFolder(SceneFolder);
            NewScene();

            var canvasGo = NewCanvas("UI");
            var ct = canvasGo.transform;
            Background(ct).GetComponent<Image>().raycastTarget = false;

            // ---- panneau marque
            var brand = Stretch("Marque", ct);
            var brandStack = Rect("Contenu", brand, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 600));
            var bv = brandStack.gameObject.AddComponent<VerticalLayoutGroup>();
            bv.childAlignment = TextAnchor.MiddleCenter;
            bv.spacing = 18;
            bv.childControlWidth = bv.childControlHeight = true;
            bv.childForceExpandWidth = true;
            bv.childForceExpandHeight = false;
            brandStack.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Logo(Elem("Logo", brandStack, 150), 150);
            Label(Elem("Titre", brandStack, 130), "Deco<color=#C8734A>AR</color>", 110, Cream, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
            Label(Elem("Slogan", brandStack, 110), "Décore ton intérieur\nen réalité augmentée", 40, Gold, TextAlignmentOptions.Center);

            var details = Rect("Atouts", brandStack, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 190));
            Fixed(details, 560, 190);
            var dv = details.gameObject.AddComponent<VerticalLayoutGroup>();
            dv.padding = new RectOffset(70, 0, 30, 0);
            dv.spacing = 10;
            dv.childControlWidth = dv.childControlHeight = true;
            dv.childForceExpandHeight = false;
            foreach (var line in new[] { "Essaie les meubles chez toi en AR", "Taille réelle, lumière réaliste", "Connexion par reconnaissance faciale" })
                Label(Elem("Atout", details, 44), $"<color=#C8734A>•</color>  {line}", 30, Cream, TextAlignmentOptions.Left);

            // ---- carte formulaire
            var cardArea = Stretch("Zone carte", ct);
            var card = CardRect(cardArea, 880);

            var screen = canvasGo.AddComponent<AuthScreen>();
            Set(screen, "scaler", canvasGo.GetComponent<CanvasScaler>());
            Set(screen, "brandPanel", brand);
            Set(screen, "brandDetails", details.gameObject);
            Set(screen, "cardArea", cardArea);

            BuildLoading(card, screen);
            BuildRegister(card, screen);
            BuildEnroll(card, screen);
            BuildVerify(card, screen);
            BuildLogin(card, screen);
            BuildFaceLogin(card, screen);
            BuildForgot(card, screen);

            AddEventSystem();
            SaveScene(AuthScenePath, first: true);
        }

        static void BuildLoading(RectTransform card, AuthScreen screen)
        {
            var panel = Stretch("Chargement", card);
            var label = Label(panel, "", 36, Muted, TextAlignmentOptions.Center);
            Set(screen, "loadingPanel", panel.gameObject);
            Set(screen, "loadingLabel", label);
        }

        static void BuildRegister(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Inscription", card, out var panel);
            Set(screen, "registerPanel", panel);

            Title(content, "Créer un compte", "Rejoins DecoAR en quelques secondes");

            var names = Row("Nom complet", content, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "firstName", Field(names, "Prenom", "Prénom", TMP_InputField.ContentType.Name, false));
            Set(screen, "lastName", Field(names, "Nom", "Nom", TMP_InputField.ContentType.Name, false));
            Set(screen, "email", Field(content, "Email", "Adresse e-mail", TMP_InputField.ContentType.EmailAddress, false));
            var phone = Field(content, "Telephone", "Téléphone (optionnel)", TMP_InputField.ContentType.Custom, false);
            phone.Input.keyboardType = TouchScreenKeyboardType.PhonePad;
            Set(screen, "phone", phone);
            var pw = Row("Mots de passe", content, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "password", Field(pw, "MotDePasse", "Mot de passe", TMP_InputField.ContentType.Password, true));
            Set(screen, "confirmPassword", Field(pw, "Confirmation", "Confirmer", TMP_InputField.ContentType.Password, true));

            // jauge de robustesse
            var strength = Elem("Robustesse", content, 40);
            var bars = Rect("Barres", strength, new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(340, 10));
            var bh = bars.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.spacing = 8;
            bh.childControlWidth = bh.childControlHeight = true;
            bh.childForceExpandWidth = bh.childForceExpandHeight = true;
            var barImages = Enumerable.Range(0, 4).Select(i => Img(Rect($"Barre {i + 1}", bars, Vector2.zero, Vector2.zero, Vector2.zero), Rounded, FieldBorder, 5f)).ToArray();
            SetArray(screen, "strengthBars", barImages);
            var strengthLabel = Rect("Texte", strength, new Vector2(1, 0.5f), new Vector2(-230, 0), new Vector2(460, 40));
            Set(screen, "strengthLabel", Label(strengthLabel, "", 24, Muted, TextAlignmentOptions.Right));

            Label(Elem("Info visage", content, 60), "Étape suivante : on enregistre ton visage pour que tu puisses te connecter en regardant la caméra.", 24, Hint, TextAlignmentOptions.Center);

            Set(screen, "registerStatus", Status(content));
            Set(screen, "registerButton", PrimaryButton(content, "Continuer"));
            Set(screen, "goToLoginButton", LinkButton(content, "Déjà un compte ? <b><color=#C8734A>Se connecter</color></b>"));
        }

        static void BuildVerify(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Verification", card, out var panel);
            Set(screen, "verifyPanel", panel);

            Set(screen, "verifySubtitle", Title(content, "Vérifie ton e-mail", ""));
            Set(screen, "verifyCode", CodeField(content, "Code"));
            Label(Elem("Aide", content, 40), "Le code est valable 15 minutes. Pense à regarder les spams.", 24, Hint, TextAlignmentOptions.Center);
            Set(screen, "verifyStatus", Status(content));
            Set(screen, "verifyButton", PrimaryButton(content, "Valider"));
            Set(screen, "resendButton", LinkButton(content, "Pas reçu ? <b><color=#C8734A>Renvoyer le code</color></b>"));
            Set(screen, "verifySwitchAccountButton", LinkButton(content, "Utiliser un autre compte"));
            panel.SetActive(false);
        }

        // Inscription, etape 2 : 3 photos du visage, analysees par OpenCV sur le serveur
        static void BuildEnroll(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Visage", card, out var panel);
            Set(screen, "enrollPanel", panel);
            Title(content, "Enregistre ton visage", "Un petit défi prouve que tu es bien une vraie personne : suis la flèche. Ton visage servira ensuite à te connecter.");
            Set(screen, "enrollChallenge", ChallengeBlock(content, "Retour au formulaire"));
            Set(screen, "enrollStatus", Status(content));
            panel.SetActive(false);
        }

        static void BuildFaceLogin(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Connexion visage", card, out var panel);
            Set(screen, "faceLoginPanel", panel);
            Set(screen, "faceLoginSubtitle", Title(content, "Connexion par le visage", ""));
            Set(screen, "faceLoginChallenge", ChallengeBlock(content, "<b><color=#C8734A>Retour</color></b>"));
            Set(screen, "faceLoginStatus", Status(content));
            panel.SetActive(false);
        }

        static void BuildLogin(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Connexion", card, out var panel);
            Set(screen, "loginPanel", panel);

            Title(content, "Bon retour !", "Connecte-toi pour continuer ta déco");
            Set(screen, "loginEmail", Field(content, "Email", "Adresse e-mail", TMP_InputField.ContentType.EmailAddress, false));
            Set(screen, "loginPassword", Field(content, "MotDePasse", "Mot de passe", TMP_InputField.ContentType.Password, true));
            Set(screen, "forgotLinkButton", LinkButton(content, "<color=#C8734A>Mot de passe oublié ?</color>", TextAlignmentOptions.Right, 44));

            Set(screen, "loginStatus", Status(content));
            Set(screen, "loginButton", PrimaryButton(content, "Se connecter"));

            // "ou" + reconnaissance faciale
            var divider = Elem("Separateur", content, 40);
            Img(Rect("Trait G", divider, new Vector2(0, 0.5f), new Vector2(170, 0), new Vector2(340, 2)), null, FieldBorder, 1f);
            Img(Rect("Trait D", divider, new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(340, 2)), null, FieldBorder, 1f);
            Label(divider, "ou", 26, Muted, TextAlignmentOptions.Center);
            Set(screen, "faceLoginButton", OutlineButton(content, "Se connecter avec mon visage", Accent, out _));

            Set(screen, "goToRegisterButton", LinkButton(content, "Pas encore de compte ? <b><color=#C8734A>S'inscrire</color></b>"));
            panel.SetActive(false);
        }

        static void BuildForgot(RectTransform card, AuthScreen screen)
        {
            var content = ScrollPanel("Mot de passe oublie", card, out var panel);
            Set(screen, "forgotPanel", panel);

            Title(content, "Mot de passe oublié", "On t'envoie un code par e-mail pour en choisir un nouveau");
            Set(screen, "forgotEmail", Field(content, "Email", "Adresse e-mail", TMP_InputField.ContentType.EmailAddress, false));
            Set(screen, "forgotSendButton", OutlineButton(content, "Recevoir un code", Accent, out _, 84));

            var step2 = Stack("Etape 2", content);
            Set(screen, "forgotStep2", step2.gameObject);
            Set(screen, "forgotCode", CodeField(step2, "Code"));
            var pw = Row("Nouveau mot de passe", step2, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "forgotPassword", Field(pw, "MotDePasse", "Nouveau mot de passe", TMP_InputField.ContentType.Password, true));
            Set(screen, "forgotConfirm", Field(pw, "Confirmation", "Confirmer", TMP_InputField.ContentType.Password, true));
            Set(screen, "forgotResetButton", PrimaryButton(step2, "Changer le mot de passe"));

            Set(screen, "forgotStatus", Status(content));
            Set(screen, "forgotBackButton", LinkButton(content, "<b><color=#C8734A>Retour à la connexion</color></b>"));
            panel.SetActive(false);
        }

        // ---------------------------------------------------------------- scene Profil

        [MenuItem("DecoAR/Auth/Creer la scene profil")]
        public static void BuildProfileScene()
        {
            LoadSprites();
            EnsureFolder(SceneFolder);
            NewScene();

            var canvasGo = NewCanvas("UI");
            var ct = canvasGo.transform;
            Background(ct).GetComponent<Image>().raycastTarget = false;
            var card = CardRect(ct, 1000);
            var content = ScrollPanel("Profil", card, out _);
            var screen = canvasGo.AddComponent<ProfileScreen>();

            // ---- en-tete : retour, photo, nom
            Set(screen, "backButton", LinkButton(content, "<b><color=#C8734A>‹ Retour</color></b>", TextAlignmentOptions.Left, 50));
            var head = Elem("Photo", content, 230);
            var photo = RoundPhoto(head, 210, out var initials);
            Set(screen, "photo", photo);
            Set(screen, "initials", initials);
            Set(screen, "changePhotoButton", LinkButton(content, "<b><color=#C8734A>Changer la photo</color></b>", TextAlignmentOptions.Center, 50));
            Set(screen, "emailLabel", Label(Elem("Email", content, 44), "", 28, Muted, TextAlignmentOptions.Center));

            // ---- informations
            Section(content, "Mes informations");
            var names = Row("Nom complet", content, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "firstName", Field(names, "Prenom", "Prénom", TMP_InputField.ContentType.Name, false));
            Set(screen, "lastName", Field(names, "Nom", "Nom", TMP_InputField.ContentType.Name, false));
            var phone = Field(content, "Telephone", "Téléphone (optionnel)", TMP_InputField.ContentType.Custom, false);
            phone.Input.keyboardType = TouchScreenKeyboardType.PhonePad;
            Set(screen, "phone", phone);
            Set(screen, "infoStatus", Status(content));
            Set(screen, "saveButton", PrimaryButton(content, "Enregistrer"));

            // ---- mot de passe
            Section(content, "Mot de passe");
            Set(screen, "currentPassword", Field(content, "Actuel", "Mot de passe actuel", TMP_InputField.ContentType.Password, true));
            var pw = Row("Nouveau", content, 84, TextAnchor.MiddleCenter, 20);
            Set(screen, "newPassword", Field(pw, "Nouveau", "Nouveau mot de passe", TMP_InputField.ContentType.Password, true));
            Set(screen, "confirmPassword", Field(pw, "Confirmation", "Confirmer", TMP_InputField.ContentType.Password, true));
            Set(screen, "passwordStatus", Status(content));
            Set(screen, "passwordButton", OutlineButton(content, "Changer le mot de passe", Accent, out _));

            // ---- visage (reconnaissance faciale)
            Section(content, "Mon visage");
            Set(screen, "faceInfo", Label(Elem("Info", content, 70), "", 26, Muted, TextAlignmentOptions.Left));
            Set(screen, "faceButton", OutlineButton(content, "Réenregistrer mon visage", Accent, out var faceLabel));
            Set(screen, "faceButtonLabel", faceLabel);
            Set(screen, "faceStatus", Status(content));

            // ---- deconnexion
            Elem("Espace", content, 20);
            Set(screen, "logoutButton", OutlineButton(content, "Se déconnecter", Danger, out _));

            // ---- camera en surimpression (visage ou photo de profil)
            var overlay = Stretch("Camera", ct);
            Img(overlay, null, new Color(0, 0, 0, 0.6f), 1f);
            var cameraCard = CardRect(overlay, 760);
            var cameraContent = ScrollPanel("Contenu", cameraCard, out _);
            Title(cameraContent, "Nouvelle photo", "Place ton visage dans le cercle, bien éclairé");
            Set(screen, "cameraTitle", cameraContent.Find("Titre").GetComponentInChildren<TMP_Text>());
            Set(screen, "cameraPanel", overlay.gameObject);
            Set(screen, "photoCapture", SelfieBlock(cameraContent, "Annuler"));
            Set(screen, "faceChallenge", ChallengeBlock(cameraContent, "Annuler"));
            overlay.gameObject.SetActive(false);

            AddEventSystem();
            SaveScene(ProfileScenePath, first: false);
        }

        // ---------------------------------------------------------------- scene

        static void NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgBottom;
        }

        static void AddEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static void SaveScene(string path, bool first)
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            // Auth = premiere scene du build : l'app s'ouvre sur l'inscription / connexion
            if (first) scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            else scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Auth] Scene enregistree : {path}");
        }
    }
}
