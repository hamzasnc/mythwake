# Shop-Katalog

Der Shop ist im internen Alpha-Build eine nicht-transaktionale Präsentation. Der Client zeigt die vorhandenen Angebote und öffnet nur eine Kaufvorschau; Echtgeldkäufe und Unity IAP bleiben ausdrücklich deaktiviert.

Die Angebotsmetadaten liegen serverseitig in `common.shop_offer_definitions` und werden über den Definitions-Snapshot als `shopOffers` ausgeliefert. Der Fallback-Katalog im Go-Backend hält den gleichen Vertrag für lokale Tests ohne PostgreSQL. `tab` plus `offerId` bilden den stabilen Schlüssel.

Serverseitig steuerbar sind derzeit Tab, Reihenfolge, sichtbare Kennzeichnung (`topPick`/`badgeLabel`) und die Präsentationsdaten. Der Client verwendet die Flags für die vorhandenen Angebots-Ribbons, vertraut ihnen aber nicht als Kauf- oder Reward-Autorisierung.

Bei Änderungen sind Migration, Fallback-Katalog, Snapshot-Test und Unity-DTO gemeinsam zu prüfen. Vor einer echten Monetarisierung braucht es zusätzlich Store-Produkt-IDs, Receipt-/Purchase-Token-Prüfung, idempotente Gewährung sowie Refund-/Restore-Verhalten.
