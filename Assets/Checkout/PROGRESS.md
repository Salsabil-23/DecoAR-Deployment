# M5 — Caisse : progression
**Menu** : `DecoAR/M5/Tout installer (1 → 4)` puis Cmd+S (ou menus 1→4 un par un).
1. Vendeur : Humanoid + `Animations/VendorAnimator.controller` (Waving en boucle), regarde le joueur, bulle FR.
2. Décor : tapis, panneau CAISSE + spot, étagère déco, plantes, présentoir colis, suspensions.
3. Panier 3D (`Basket3D`) + colis `CartTon` animé (`ParcelAnimation` → rebond, vibration, son, envol).
4. `M5_Player` (Main Camera dedans, ZQSD + clic droit / joystick tactile) + `M5_CheckoutUI` (catalogue démo, commande en TND, Confirmer/Vider, toasts). L'UI de commande apparaît près du comptoir et la vue se tourne vers le panier.
**À la main** : son de confirmation (.wav/.mp3 dans Assets/Checkout, puis relancer 3). Si un objet n'est pas trouvé : le sélectionner + relancer le menu.
**Intégration autres modules** : `FindAnyObjectByType<M5CheckoutUI>().AddProduct(nom, prixTND, couleur, prefab)`.
