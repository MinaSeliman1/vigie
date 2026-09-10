# Plan de facturation

La démo et le premier pilote restent gratuits. La facturation ne doit être activée qu’après validation du modèle commercial, du compte marchand et des obligations fiscales.

## Décision technique proposée

Stripe Checkout et Stripe Billing sont le choix proposé pour la première version payante : ils couvrent l’abonnement, l’essai, le portail client et les webhooks signés sans faire transiter les données de carte dans Vigie. Le coût et les conditions doivent être confirmés dans le compte Stripe avant toute annonce; la page [Stripe Checkout Canada](https://stripe.com/en-ca/payments/checkout) présente le fonctionnement et les tarifs affichés par le fournisseur.

## Préparation obligatoire

- définir les plans (pilote, centre, réseau), le nombre de piscines, les limites d’équipe et les fonctionnalités incluses;
- choisir la devise, la période d’essai, la date de facturation, les remboursements, les annulations et la procédure de suspension;
- vérifier l’identité de l’entreprise dans Stripe et utiliser séparément les clés de test et de production;
- configurer un endpoint webhook avec vérification de signature et idempotence (`checkout.session.completed`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.payment_failed`);
- conserver seulement l’identifiant client, l’identifiant d’abonnement, le plan et l’état de facturation dans Vigie;
- afficher le prix, les taxes et les conditions de résiliation avant l’achat, puis offrir le portail client pour gérer l’abonnement.

## Taxes et responsabilités

Vigie doit confirmer son obligation d’inscription à la TPS/TVQ et la façon de présenter les taxes. Revenu Québec indique notamment le seuil de petit fournisseur de 30 000 $ dans les périodes prévues par ses règles; consulter la [page officielle d’inscription TPS/TVQ](https://www.revenuquebec.ca/fr/entreprises/taxes/tpstvh-et-tvq/inscription-aux-fichiers-de-la-tps-et-de-la-tvq/) et faire valider les prix par un comptable.

## Activation dans le produit

Ne pas simuler un paiement dans l’interface publique. Après validation du modèle, implémenter un état d’abonnement côté serveur, protéger les fonctionnalités par des entitlements testés, enregistrer les transitions dans l’audit et exécuter un test de bout en bout en mode Stripe test. L’activation en production est une étape manuelle du responsable commercial.
