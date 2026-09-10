# Réponse aux incidents

Cette procédure est le point de départ pour un pilote commercial. Elle doit être adaptée au contrat, aux coordonnées réelles de l’équipe et aux obligations applicables avant l’ouverture d’un centre.

## 1. Détecter et qualifier

Créer un incident dès qu’une alerte de disponibilité, une erreur répétée, un accès suspect, une fuite potentielle ou une demande d’un centre l’exige. Conserver l’heure en UTC, le lien vers le déploiement, le `X-Request-Id`, le périmètre touché et la personne qui prend la responsabilité.

| Niveau | Exemple | Délai de prise en charge |
| --- | --- | ---: |
| P1 critique | Données accessibles à la mauvaise organisation, compromission de secret ou service inutilisable | 15 min |
| P2 majeur | Fonction métier bloquée pour un centre ou persistance indisponible | 1 h |
| P3 mineur | Dégradation contournable ou erreur d’affichage | 1 jour ouvrable |

## 2. Contenir

- arrêter un déploiement en cours et conserver son commit;
- révoquer les sessions en réinitialisant le secret JWT si une clé ou un jeton est suspect;
- désactiver l’invitation, le membre ou le compte concerné;
- limiter temporairement l’origine autorisée ou le fournisseur de courriel si nécessaire;
- préserver les journaux, les identifiants de corrélation et l’audit sans copier de mot de passe ou de jeton dans un ticket.

## 3. Rétablir

1. Vérifier `/health/ready`, la connexion PostgreSQL et les derniers déploiements.
2. Si la donnée est en cause, suivre le [runbook de sauvegarde et restauration](backup-restore.md) dans une base temporaire.
3. Déployer un correctif avec les tests du domaine, les tests d’intégration et le build frontend verts.
4. Vérifier le parcours de connexion, le calendrier, l’assignation, l’échange, l’export et la séparation entre organisations.
5. Confirmer le rétablissement au responsable du centre avec la période touchée et les mesures prises.

## 4. Évaluer une atteinte à la vie privée

Documenter les données concernées, les personnes touchées, la probabilité de préjudice et les mesures de réduction du risque. Au Canada, une atteinte présentant un risque réel de préjudice important peut exiger un avis aux personnes touchées et au Commissariat; les incidents doivent aussi être consignés selon la procédure applicable. Consulter les [directives officielles sur les atteintes](https://www.priv.gc.ca/en/report-a-concern/report-a-privacy-breach-at-your-organization/report-a-privacy-breach-at-your-business/?referral=79482820) et faire valider la décision par la personne responsable de la vie privée.

## 5. Clôturer et apprendre

Le rapport de clôture contient la chronologie, la cause, les organisations touchées, les secrets révoqués, les données restaurées, les communications, les tests de non-régression et les actions préventives avec un responsable et une date cible. Ne fermer l’incident qu’après vérification indépendante du correctif.
