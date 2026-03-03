# Configuration de la Box d'Information de Carte

## Instructions de Configuration dans Unity Editor

### Étape 1 : Créer le GameObject CardInfoBox

1. Dans la scène où se trouve la collection de cartes (probablement une des scènes dans `Assets/Scenes/`), créer un Canvas s'il n'existe pas déjà
2. Sous le Canvas, créer un nouveau GameObject et le nommer "CardInfoBox"
3. Ajouter le composant `CardInfoBox` au GameObject

### Étape 2 : Créer la Structure UI

Sous le GameObject "CardInfoBox", créer la hiérarchie suivante :

```
CardInfoBox
└── InfoPanel (Panel)
    ├── Background (Image)
    ├── CardImage (Image)
    ├── CardName (TextMeshProUGUI)
    ├── Description (TextMeshProUGUI)
    ├── QuantityOwned (TextMeshProUGUI)
    ├── TotalPCEarned (TextMeshProUGUI)
    ├── FirstTimeValueLabel (TextMeshProUGUI)
    └── SubsequentValueLabel (TextMeshProUGUI)
```

### Étape 3 : Configuration Détaillée des Composants

#### InfoPanel (Panel)
- **RectTransform** :
  - Width: 400
  - Height: 600
  - Anchor: Middle Center
- **Image** :
  - Color: Noir avec Alpha à 0.9 (pour un fond semi-transparent)
  - Ou utiliser un sprite de fond personnalisé

#### Background (Image) - Optionnel
- Sprite de fond décoratif
- Peut être utilisé pour donner un style visuel à la box

#### CardImage (Image)
- **RectTransform** :
  - AnchorMin: (0.5, 1)
  - AnchorMax: (0.5, 1)
  - Pivot: (0.5, 1)
  - Position Y: -20
  - Width: 250
  - Height: 250
- **Image** :
  - Preserve Aspect: Coché

#### CardName (TextMeshProUGUI)
- **RectTransform** :
  - AnchorMin: (0.5, 1)
  - AnchorMax: (0.5, 1)
  - Pivot: (0.5, 1)
  - Position Y: -290
  - Width: 350
  - Height: 40
- **TextMeshProUGUI** :
  - Font Size: 24
  - Font Style: Bold
  - Alignment: Center
  - Color: Blanc ou couleur de votre choix

#### Description (TextMeshProUGUI)
- **RectTransform** :
  - AnchorMin: (0.5, 1)
  - AnchorMax: (0.5, 1)
  - Pivot: (0.5, 1)
  - Position Y: -350
  - Width: 350
  - Height: 100
- **TextMeshProUGUI** :
  - Font Size: 16
  - Alignment: Top Center
  - Color: Blanc
  - Word Wrapping: Activé

#### QuantityOwned (TextMeshProUGUI)
- **RectTransform** :
  - AnchorMin: (0.5, 1)
  - AnchorMax: (0.5, 1)
  - Pivot: (0.5, 1)
  - Position Y: -470
  - Width: 350
  - Height: 30
- **TextMeshProUGUI** :
  - Font Size: 18
  - Font Style: Bold
  - Alignment: Center
  - Color: Cyan (#00BCD4)

#### TotalPCEarned (TextMeshProUGUI)
- **RectTransform** :
  - AnchorMin: (0.5, 1)
  - AnchorMax: (0.5, 1)
  - Pivot: (0.5, 1)
  - Position Y: -505
  - Width: 350
  - Height: 30
- **TextMeshProUGUI** :
  - Font Size: 18
  - Font Style: Bold
  - Alignment: Center
  - Color: Or (#FFD700)

#### FirstTimeValueLabel (TextMeshProUGUI)
- **RectTransform** :
  - AnchorMin: (0, 0)
  - AnchorMax: (0, 0)
  - Pivot: (0, 0)
  - Position X: 25
  - Position Y: 60
  - Width: 350
  - Height: 30
- **TextMeshProUGUI** :
  - Font Size: 18
  - Alignment: Left
  - Color: Vert clair (#4CAF50) pour indiquer un gain

#### SubsequentValueLabel (TextMeshProUGUI)
- **RectTransform** :
  - AnchorMin: (0, 0)
  - AnchorMax: (0, 0)
  - Pivot: (0, 0)
  - Position X: 25
  - Position Y: 25
  - Width: 350
  - Height: 30
- **TextMeshProUGUI** :
  - Font Size: 18
  - Alignment: Left
  - Color: Jaune clair (#FFC107) pour différencier

### Étape 4 : Assigner les Références dans CardInfoBox

Dans l'Inspector du GameObject "CardInfoBox", avec le composant CardInfoBox :
1. **Card Image** : Glisser l'objet CardImage
2. **Card Name Text** : Glisser l'objet CardName
3. **Description Text** : Glisser l'objet Description
4. **First Time Value Text** : Glisser l'objet FirstTimeValueLabel
5. **Subsequent Value Text** : Glisser l'objet SubsequentValueLabel
6. **Quantity Owned Text** : Glisser l'objet QuantityOwned
7. **Total PC Earned Text** : Glisser l'objet TotalPCEarned
8. **Info Panel** : Glisser l'objet InfoPanel

### Étape 5 : Configuration du Canvas

Assurez-vous que le Canvas a :
- **Render Mode** : Screen Space - Overlay (ou Camera si vous préférez)
- **Canvas Scaler** : Scale With Screen Size
  - Reference Resolution: 1080x1920 (ou selon votre résolution cible)
  - Match: 0.5 (entre Width et Height)

### Étape 6 : Ordre de Rendu

Le CardInfoBox doit être au-dessus des autres éléments UI :
- Dans la hiérarchie, placer CardInfoBox après (en dessous de) les autres éléments UI
- Ou utiliser un Canvas séparé avec un Sort Order plus élevé

### Amélioration Optionnelle : Animation

Pour une meilleure expérience utilisateur, vous pouvez ajouter :

1. **Composant CanvasGroup** sur InfoPanel :
   - Permet de contrôler l'alpha pour un fade in/out

2. **Animation LeanTween** dans le code :
   Modifier `ShowCardInfo` et `HideInfoBox` dans CardInfoBox.cs pour ajouter des animations :

```csharp
public void ShowCardInfo(CardData cardData, Vector3 position)
{
    // ... code existant ...
    
    if (infoPanel != null)
    {
        infoPanel.SetActive(true);
        
        // Animation fade in
        CanvasGroup canvasGroup = infoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            LeanTween.alphaCanvas(canvasGroup, 1f, 0.2f);
        }
    }
}

public void HideInfoBox()
{
    if (infoPanel != null)
    {
        CanvasGroup canvasGroup = infoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, 0.15f).setOnComplete(() => {
                infoPanel.SetActive(false);
            });
        }
        else
        {
            infoPanel.SetActive(false);
        }
    }
}
```

### Étape 7 : Test

1. Lancer la scène
2. Survoler une carte dans la collection
3. La box d'information devrait apparaître avec :
   - L'image de la carte
   - Le nom de la carte
   - La description
   - Le nombre de cartes possédées
   - Le total de PC gagnés avec cette carte
   - Les PC de première obtention
   - Les PC des obtentions suivantes

## Personnalisation

Vous pouvez personnaliser :
- Les couleurs des textes et du fond
- La taille et la position de la box
- Ajouter des icônes (par exemple, une icône PC à côté des valeurs)
- Ajouter plus d'informations (rareté, élément, etc.)

## Notes

- La box utilise un système singleton pour être accessible depuis n'importe quel CardUI
- Les événements de pointer (OnPointerEnter/OnPointerExit) détectent automatiquement le survol de la souris
- Sur mobile, le survol ne fonctionnera pas comme sur PC ; vous pouvez modifier CardUI pour afficher la box sur un appui long ou un double tap
