# 11 · États et écrans

## États de chaque composant

| État | Rendu |
| --- | --- |
| Repos | Couleurs du thème |
| Pressé | Assombri 14 % + échelle 0,97, 90 ms |
| Sélectionné | `state/selected` (laiton sur verre, terracotta sur papier) ; carte : liseré 4 |
| Focus (clavier, manette) | Anneau `state/focus-ring` de 3 à 4 de distance |
| Désactivé | Opacité 55 %, pas de vibration, pas de son |
| Chargement | Anneau pointillé qui tourne à la place du libellé |
| Erreur | Brique (300 sur verre, 700 sur papier) + icône `alerte` + message |
| Succès | Sauge 300 (verre) / succès 700 (papier) + icône `valide` |

## États d'écran

| Cas | Illustration | Composant |
| --- | --- | --- |
| Panier vide | `panier-vide` | État vide + « Voir le catalogue » |
| Catalogue 3D vide (M6) | `catalogue-3d-vide` | État vide + « + Générer un modèle 3D » |
| Modèle pas encore généré | `modele-3d-attente` | Dans la visionneuse |
| Aucun résultat | `aucun-resultat` | Dans le tiroir de recherche + suggestions de rayons |
| Aucune photo (M6) | `aucune-photo` | Étape 1 |
| Caméra refusée | `camera-refusee` | Plein écran + « Ouvrir les réglages » |
| Hors ligne | `hors-ligne` | Plein écran ou toast selon l'écran |
| Paiement confirmé | `paiement-confirme` | Fenêtre après l'animation de la caisse |

## Thème par écran

| Écran | Thème | Notes |
| --- | --- | --- |
| Splash | Papier | `assets/splash` |
| Connexion, inscription, code, mot de passe oublié | Papier | Logo vertical en haut (portrait) ou à gauche (paysage) |
| Verrouillage | Papier | Ovale + coins de visée + « Pour ta sécurité, reconnecte-toi » |
| Profil | Papier | En-tête de profil + liste de lignes |
| Personnage (M1) | Papier, aperçu 3D dans une visionneuse | Onglets en pilules, pastilles de couleur 88 |
| Scan du visage | Verre (sur caméra) | Ovale + coins de visée |
| Boutique 3D (M5) | Verre | HUD |
| Catalogue (M3) | Verre | Onglets de rayons, cartes, fiche produit |
| AR (M4) | Verre | Réticule, consignes, outils |
| Paiement | Papier (fenêtre) | Au-dessus de la boutique, voile 40 % |
| Administration (M6) | Papier | Remplace le fond brun ; visionneuse en dégradé sombre pour faire ressortir le modèle |

## Plans d'écran (paysage 1920 × 1080)

```
BOUTIQUE (M5)                                     CATALOGUE (M3)
┌───────────────────────────────────────────┐     ┌───────────────────────────────────────────┐
│ [Profil][⚙][👁]     ( toast )     [✦][🛍³] │     │ [‹ Boutique]  Salon Chambre Cuisine…  [🔍][🛍]│
│                                           │     │                                           │
│                3D                         │     │           étagère 3D           ┌────────┐ │
│                                           │     │                                │ fiche  │ │
│ (joystick)                     (regarder) │     │                                │ produit│ │
│              [ Payer · 1 890 TND › ]      │     │ [carte][carte][carte][carte]…  └────────┘ │
└───────────────────────────────────────────┘     └───────────────────────────────────────────┘

AR (M4)
┌───────────────────────────────────────────┐
│ [‹ Rayon][Boutique]  [Fauteuil Lina      ]   [?][🛍]│
│                      [● Taille réelle    ]       │
│                                     [◐ Coloris]  │
│            ( réticule )             [⤡ Taille ]  │
│                                     [◎ Photo  ]  │
│   ( ● Balaie lentement le sol )                  │
│              [ Ajouter au panier · 890 TND ]     │
└───────────────────────────────────────────┘
```

Repères : marge 48 + zone sûre ; HUD à 112 ; écart 20 ; tiroirs 760.
