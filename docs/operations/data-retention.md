# Conservation et suppression des données

Ce document propose une politique opérationnelle pour un premier pilote Vigie. Les durées sont des valeurs de départ à faire approuver par l’organisation cliente et à valider avec un conseiller juridique avant d’importer des données réelles.

## Matrice de conservation proposée

| Donnée | Utilité | Durée proposée après la fin du service | Responsable |
| --- | --- | ---: | --- |
| Profil, membres et rattachements | Authentification et administration | 30 jours, puis suppression ou anonymisation | Responsable de l’organisation |
| Quarts, assignations et échanges | Paie, planification et preuve opérationnelle | 24 mois | Responsable de l’organisation |
| Certifications et échéances | Vérification de l’admissibilité | 24 mois après le dernier quart associé | Responsable de l’organisation |
| Disponibilités | Préparation des quarts | 12 mois | Responsable de l’organisation |
| Journal d’audit | Traçabilité et enquêtes | 24 mois minimum, selon les obligations applicables | Responsable de l’organisation |
| Invitations et jetons de récupération | Activation et sécurité | 7 jours pour une invitation; 30 minutes pour une récupération | Vigie, nettoyage automatique |
| Journaux techniques | Disponibilité et diagnostic | 30 jours | Vigie |
| Sauvegardes chiffrées | Reprise après incident | Selon la rotation définie dans le contrat d’exploitation | Vigie / fournisseur |

## Règles d’exploitation

1. Le responsable de l’organisation confirme les durées et les obligations de conservation avant l’ouverture du pilote.
2. Une demande d’accès, de correction ou de suppression est vérifiée et inscrite dans l’audit; l’export de compte exclut les secrets.
3. La suppression d’un compte désactive ses rattachements et anonymise son identité dans l’audit lorsque l’historique doit être conservé.
4. Les sauvegardes sont chiffrées, limitées aux personnes autorisées et supprimées selon leur date d’expiration. Une sauvegarde PostgreSQL ne contient pas les objets binaires de Storage : ceux-ci doivent être traités séparément.
5. Toute exception est documentée avec sa justification, sa date d’expiration et la personne qui l’a approuvée.

## Contrôle mensuel

- vérifier les comptes désactivés et les invitations expirées;
- vérifier les exports et demandes de suppression encore ouverts;
- vérifier que les journaux techniques ne contiennent pas de secret ou de donnée personnelle inutile;
- vérifier la date d’expiration des sauvegardes et supprimer les copies hors politique;
- noter le résultat dans le journal d’exploitation.

Cette matrice s’appuie sur les principes de limitation de la collecte, de la conservation et des mesures de protection décrits par le [Commissariat à la protection de la vie privée du Canada](https://www.priv.gc.ca/en/privacy-topics/privacy-laws-in-canada/the-personal-information-protection-and-electronic-documents-act-pipeda/pipeda_brief?wbdisable=true). Elle ne remplace pas une analyse juridique.
