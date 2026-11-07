# 01 · Principes

## L'idée en une phrase

**DecoAR, c'est ton intérieur avant de l'acheter.** On entre dans une boutique chaleureuse, on choisit un meuble, et on le pose dans sa propre pièce, à la bonne taille.

Tout le système part du symbole « Maison AR » : **la maison** (chez toi), **le meuble sur son socle lumineux** (l'objet choisi), **les coins de visée** et **l'anneau** (l'AR).

## Cinq principes

1. **Chaleureux, jamais froid.**
   Pas de gris, pas de noir pur, pas de bleu « tech ». Les neutres sont des lins, le sombre est un brun, la lumière est du laiton. Même l'AR, très technique, parle le langage d'un intérieur.

2. **L'interface s'efface devant l'objet.**
   Le héros, c'est le meuble en 3D. L'interface flotte en verre fumé sur les bords, laisse le centre libre, et disparaît pendant l'inspection et la photo.

3. **Une couleur, un sens.**
   Laiton = « regarde, c'est précieux / essaie-le » (AR, prix, sélection). Sauge = « je l'achète » (panier, paiement). Terracotta = la marque et les actions de compte. L'utilisateur apprend ce code en une minute.

4. **Le geste avant le bouton, mais toujours expliqué.**
   Glisser, pincer, secouer : c'est ce qui rend DecoAR ludique. Chaque geste a son icône, son explication la première fois, et un bouton de secours.

5. **Lisible partout.**
   Sur un mur blanc filmé en plein soleil comme dans une pièce sombre. Verre à 80 %, texte ≥ 12 dp, cibles ≥ 44 dp, contraste vérifié (voir `02-couleurs.md`).

## Décisions qui unifient l'application

Réponses aux six points ouverts du document de l'application (section 10).

| # | Question | Décision |
| --- | --- | --- |
| 1 | Une seule palette | Oui. Terracotta de marque alignée sur le symbole : `#DF5F2A`, une seule échelle par teinte (`tokens/decoar-tokens.json`). |
| 2 | Deux thèmes | **Verre** (sur la 3D / la caméra : M5, M3, M4, HUD) et **Papier** (formulaires : Auth, Profil, M1, M6). M6 quitte son fond brun pour le papier. |
| 3 | Typographie | **Outfit** partout, échelle unique de 12 styles, tailles relevées aux standards iOS / Android. |
| 4 | Composants communs | Une seule version de chaque composant, construite par `DecoUI.cs` (bouton, champ, interrupteur, toast, pastille…). Spécifications : `05-composants.md`. |
| 5 | Registre | **Tutoiement partout.** C'est une expérience de jeu, déjà tutoyée à l'entrée (compte, personnage). Voir `09-redaction.md`. |
| 6 | Lisibilité AR | Jamais de texte nu sur la caméra ; verre ≥ 80 % ou voile `dc-voile-haut`. Ratios vérifiés dans `02-couleurs.md`. |

## Ce qui rend DecoAR différent (les « signatures »)

- **Les coins de visée** du symbole : ils encadrent tout ce qui est « vu par la caméra » (scan du visage, visionneuse 3D de M6, photo produit).
- **L'anneau et le socle lumineux** : comme sous le fauteuil du symbole, un anneau lumineux marque l'endroit où un objet se pose (réticule AR, icône AR, socle de M6, chargement).
- **Le halo** (dégradé terracotta → laiton) : réservé aux moments magiques (génération 3D, première pose AR). S'il est partout, il ne veut plus rien dire.
- **La lumière pêche** : les fonds papier reprennent le dégradé pêche de la plaque du symbole (dégradé `aube`).
