# M5 — Caisse : progression
**Menu** : `DecoAR/M5/Tout installer (1 → 4)` puis Cmd+S (ou menus 1→4 un par un).
1. Vendeur : Humanoid + `Animations/VendorAnimator.controller` (Waving en boucle), regarde le joueur, bulle FR.
2. Décor : tapis, panneau CAISSE + spot, étagère déco, plantes, présentoir colis, suspensions.
3. Panier 3D (`Basket3D`) + colis `CartTon` animé (`ParcelAnimation` → rebond, vibration, son, envol).
4. `M5_Player` (Main Camera dedans, ZQSD + clic droit / joystick tactile) + `M5_CheckoutUI` (catalogue démo, commande en TND, Confirmer/Vider, toasts). L'UI de commande apparaît près du comptoir et la vue se tourne vers le panier.
**À la main** : son de confirmation (.wav/.mp3 dans Assets/Checkout, puis relancer 3). Si un objet n'est pas trouvé : le sélectionner + relancer le menu.
**Intégration autres modules** : `FindAnyObjectByType<M5CheckoutUI>().AddProduct(nom, prixTND, couleur, prefab)`.

## Nouveautés (menus 5 → 8)
**Menu** : `DecoAR/M5/Installer les nouveautés (5 → 7)` puis Cmd+S.
5. **Aj = vendeur** : skin Mixamo extrait (`Characters/Aj_Textures`, `Aj_Materials`, URP Lit), `Animations/AjAnimator.controller` : Idle (copie de M1) → Salut quand on approche → Applaudit + Joie après la commande. X Bot est désactivé (pas supprimé).
6. **Télévision** : `Materials/M5_TV.mat` (couleur + métal/brillance + relief), écran `M5_EcranTV` (`M5TVScreen`) : s'allume à l'approche (effet cathodique, lueur, son) → « Voir le catalogue ». Toucher l'écran → événement `onCatalogRequested` (à brancher sur le catalogue).
7. **Ambiance** : `Settings/M5_URP.asset` (copie du pipeline avec lumières d'appoint + HDR, activé seulement dans la caisse par `M5Ambiance`), post-traitement `Settings/M5_PostProcess.asset`, lumière chaude + brume légère.
8. **Téléphone** : build de la caisse seule (Android : installe et lance ; iOS : projet Xcode dans `Builds/`).
**Mobile** : gyroscope (tourner le téléphone pour regarder, boutons ON/OFF + Recentrer), paysage forcé, 60 FPS, zone sûre (encoche).
**Paiement** : adresse de livraison obligatoire, bouton « Utiliser ma position (GPS) » → adresse via OpenStreetMap (Nominatim). Dans l'éditeur : position de test (ESPRIT).

## Paiement Stripe (Supabase)
- Serveur : `supabase/functions/stripe-checkout/index.ts` (racine du dépôt). Secrets Supabase : `STRIPE_SECRET_KEY` (obligatoire), `STRIPE_CURRENCY` (défaut `eur`), `TND_RATE` (défaut 0.30 : 1 TND → devise Stripe).
- Déploiement : `supabase functions deploy stripe-checkout --no-verify-jwt`.
- Unity : menu `DecoAR/M5/9. Paiement Stripe (Supabase)` puis, sur M5_CheckoutUI, remplir `Function Url` + `Supabase Anon Key`. Sans URL, la commande est confirmée sans paiement (démo).
- ⚠️ La clé secrète Stripe (`sk_...`) ne va JAMAIS dans Unity. Démo : les prix viennent de l'app (à vérifier côté serveur en production).
- Paiement DANS l'app : `Plugins/Android/M5WebView.java` + `Plugins/iOS/M5WebView.mm` (page Stripe plein écran, fermée automatiquement au retour de Stripe, puis vérification serveur et animation). Dans l'éditeur : navigateur externe.
