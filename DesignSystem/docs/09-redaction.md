# 09 · Rédaction

## Le registre : tutoiement partout

DecoAR est une expérience de jeu. Le compte et le personnage tutoient déjà, c'est le premier contact. On aligne la boutique, le catalogue et l'AR. Une voix, celle d'un vendeur sympathique qui connaît bien la déco.

## Le ton

- **Court, concret, à l'impératif** : on dit quoi faire. « Balaie lentement le sol », pas « Veuillez effectuer un balayage ».
- **Chaleureux sans en faire trop** : un « Parfait ! » ou « Joli choix » de temps en temps, jamais d'emoji dans l'interface.
- **Honnête dans l'erreur** : ce qui s'est passé + ce qu'il faut faire. Jamais « Erreur 500 ».

## Formats

| Élément | Format |
| --- | --- |
| Prix | `1 890 TND` (espace fine insécable U+202F pour les milliers, espace insécable avant TND) |
| Dimensions | `101 × 94 × 105 cm` puis `l × p × h` en légende |
| Pourcentage | `125 %` (espace insécable) |
| Guillemets | « … » avec espaces insécables |
| En cours | points de suspension « … » (un seul caractère) |
| Majuscules | Seulement la première lettre (« Ajouter au panier », pas « Ajouter Au Panier ») |

## Libellés harmonisés (vouvoiement → tutoiement)

| Avant | Après |
| --- | --- |
| Balayez lentement le sol / le mur | Balaie lentement le sol / le mur |
| Touchez la surface pour poser le produit | Touche la surface pour poser l'objet |
| Glissez le produit avec un doigt | Glisse l'objet avec un doigt |
| Pincez ou écartez deux doigts | Pince ou écarte deux doigts |
| Faites pivoter deux doigts | Fais tourner deux doigts |
| Touchez une autre zone | Touche un autre endroit |
| Touchez un produit… | Touche un objet pour le voir de près |
| Votre modèle 3D apparaîtra ici | Ton modèle 3D apparaîtra ici |
| Ce produit se pose sur un mur | Cet objet se pose sur un mur : vise un mur |
| Ce produit se pose sur le sol | Cet objet se pose au sol : vise le sol |
| Voir chez moi en AR | Voir chez moi (bouton laiton + icône `ar`) |

## Bibliothèque de messages

### Succès
- « Visage enregistré »
- « Monstera ajouté au panier »
- « Posé ! Tu peux le déplacer du doigt »
- « Photo enregistrée dans ta galerie »
- « Paiement confirmé. Merci ! »
- « Modèle 3D prêt »

### Chargement
- « Reconnaissance en cours… »
- « Création de ton visage 3D… »
- « Génération 3D en cours… (environ 1 min) »
- « Recherche du sol… »

### Erreurs
| Situation | Message |
| --- | --- |
| Réseau | « Pas de connexion. Vérifie ton réseau puis réessaie. » |
| Serveur | « Le serveur ne répond pas. Réessaie dans un instant. » |
| Caméra refusée | « La caméra est désactivée. Autorise-la dans les réglages pour essayer en AR. » |
| Trop sombre | « Il fait trop sombre : mets-toi face à une lumière. » |
| Surface introuvable | « Je ne trouve pas de surface. Éclaire la pièce et balaie plus lentement. » |
| Champ e-mail | « Cette adresse e-mail n'est pas valide » |
| Mot de passe | « 8 caractères minimum, avec des lettres et des chiffres » |
| Session | « Ta session a expiré. Reconnecte-toi. » |
| Paiement annulé | « Paiement annulé. Ton panier est toujours là. » |
| Génération ratée | « La génération a échoué. Essaie une photo plus nette, sur un fond uni. » |

### Permissions (avant la demande système)
| Permission | Titre | Phrase |
| --- | --- | --- |
| Caméra (arrière) | Voir l'objet chez toi | « DecoAR utilise la caméra pour poser les objets dans ta pièce. Rien n'est enregistré sans ton accord. » |
| Caméra (avant) | Ton visage | « Pour te connecter d'un regard et créer ton personnage. Ton visage reste sur ton téléphone. » |
| Micro | Recherche vocale | « Dis simplement ce que tu cherches : « lampe en laiton », « plante ». » |
| Position | Livraison | « Pour remplir ton adresse automatiquement. Utilisée une seule fois. » |
| Réseau local | Connexion au serveur | « Pour générer les modèles 3D sur le serveur de la boutique. » |

> Les phrases sur la conservation des données doivent correspondre à ce que fait réellement l'application : à vérifier par l'équipe avant publication.

### États vides
Textes dans `assets/etats-vides/textes.json` (titre + phrase), alignés sur les illustrations.
