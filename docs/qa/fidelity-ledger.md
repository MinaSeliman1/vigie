# Registre de fidélité visuelle

Ce registre compare le concept de référence avec l’interface livrée pour garder une direction visuelle cohérente pendant les prochaines itérations.

## Points vérifiés

| Point | Concept de référence | Interface livrée | Résultat |
| --- | --- | --- | --- |
| Navigation | Rail latéral sombre, accent cyan, état actif très lisible | Rail responsive avec navigation principale, profil et accent cyan | Conforme |
| Surface | Grande surface blanche avec bordures fines et profondeur légère | Canvas blanc, bordures douces, rayons mesurés et ombres discrètes | Conforme |
| Hiérarchie | Calendrier comme contenu principal, alertes et échanges visibles au premier écran | Calendrier principal, métriques, alertes de certification et échanges accessibles immédiatement | Conforme |
| Typographie | Sans-serif nette, contraste élevé, titres courts | DM Sans pour l’interface et Manrope pour les titres, textes en français | Conforme |
| Responsive | Densité desktop conservée avec adaptation mobile | Navigation mobile, cartes empilées et calendrier défilable à 390 × 844 | Conforme |

## Texte au-dessus de la ligne de flottaison

Le concept et la version livrée partagent les libellés fonctionnels « Vigie », « Mon calendrier », « Équipe », « Échanges », « Certifications », « Cette semaine », « Certifications à surveiller » et « Échanges en attente ». La version livrée ajoute « Créer un quart » et les actions d’échange nécessaires au périmètre métier approuvé.

## Écarts assumés

- Le concept présente surtout la vue coordonnateur; la version livrée démarre sur la vue personnelle d’un sauveteur afin de rendre la démonstration du problème plus immédiate.
- Les données de la démo UI sont locales pour permettre une présentation instantanée. L’API JWT, le modèle EF Core et les ports de persistance sont présents pour le branchement PostgreSQL de l’itération suivante.
- Le panneau « Priorités du jour » du concept devient une zone « À surveiller » et une liste d’échanges, directement alignées sur les règles métier du MVP.

## Vérification effectuée

- Parcours desktop dans le navigateur intégré : ouverture d’un quart, demande d’échange, changement de profil coordonnateur et approbation.
- Parcours mobile en 390 × 844 : navigation, empilement des cartes et calendrier défilable.
- Console navigateur sans erreur et interface inspectée visuellement avec `view_image`.

