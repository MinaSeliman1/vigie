# Checklist de lancement en production

Cette checklist décrit le passage de la démonstration gratuite à un premier centre aquatique réel. Les étapes qui touchent un fournisseur externe restent manuelles; les migrations, les tests et les contrôles applicatifs sont automatisés par le dépôt.

## Avant d’inviter une équipe

- [ ] Créer un projet PostgreSQL de production séparé du projet de démonstration.
- [ ] Conserver la chaîne `ConnectionStrings__Vigie` et `Jwt__Key` dans le gestionnaire de secrets de Render; ne jamais les ajouter à GitHub.
- [x] Vérifier Render avec `/health/ready` et confirmer `persistence=postgresql`.
- [ ] Exécuter le [runbook de sauvegarde et restauration](backup-restore.md) sur une base temporaire avant la première importation.
- [ ] Définir la durée de conservation des données métier et de l’audit avec le responsable du centre.
- [ ] Faire relire les [conditions d’utilisation](../legal/conditions-utilisation.md) et la [politique de confidentialité](../legal/politique-confidentialite.md) par la personne responsable du centre.

## Courriels et surveillance

- [ ] Vérifier un domaine d’envoi dans Resend et configurer `Resend__ApiKey` et `Resend__From` dans Render.
- [ ] Envoyer un courriel d’invitation et un courriel de récupération avec un compte de test réel.
- [x] Ajouter une sonde gratuite vers `/health/ready` avec le workflow GitHub Actions `Vérifier les services en production` (toutes les 15 minutes, avec vérification de la persistance PostgreSQL).
- [ ] Ajouter une alerte externe facultative (UptimeRobot, Better Uptime ou équivalent) avant l’ouverture à un premier centre, puis vérifier les notifications.
- [ ] Conserver le lien public de la procédure de [support](../support.md) dans les communications de l’organisation.

## Acceptation fonctionnelle

- [ ] Créer l’organisation avec son responsable Régie aquatique.
- [ ] Vérifier le catalogue des piscines municipales, les secteurs et les périmètres de chaque chef ou chargé.
- [ ] Inviter un sauveteur, activer son compte et confirmer son calendrier personnel.
- [ ] Créer un quart brouillon, le publier, compléter sa couverture et vérifier la notification reçue.
- [ ] Déclarer une indisponibilité, demander un échange, puis vérifier l’approbation et les notifications des deux personnes.
- [ ] Tester le refus d’une certification échue et d’un chevauchement d’horaires.
- [ ] Exporter l’historique CSV et les données personnelles, puis vérifier l’absence de mot de passe ou de jeton.
- [ ] Tester la suppression d’un compte réel; pour le compte propriétaire, transférer d’abord la responsabilité de l’organisation.

## Facturation et ouverture commerciale

La V1 peut rester gratuite pour un pilote. Pour vendre des abonnements, choisir manuellement un fournisseur de paiement, ouvrir son compte marchand, vérifier son identité et définir les taxes, les prix, les remboursements et les conditions de résiliation. Cette étape ne doit pas être simulée dans Vigie : elle dépend du contrat commercial et du fournisseur choisi.

Après ce choix, ajouter les variables secrètes du fournisseur dans Render, configurer ses webhooks signés et couvrir les changements de plan par des tests d’intégration avant d’activer la facturation pour une organisation.

## Contrôle final

- [x] `dotnet test Vigie.sln --configuration Release` passe localement et dans la CI.
- [x] Depuis le dossier `frontend`, `npm run lint`, `npm run test` et `npm run build` passent localement et dans la CI.
- [x] Le workflow CI GitHub est vert sur le commit livré.
- [x] Render répond à `/health/ready` avec `persistence=postgresql` et GitHub Pages affiche **API connectée**.
- [ ] Noter la date, le commit, la version des migrations et la personne qui a réalisé l’acceptation.
