using System;

namespace OC2HudCustomization
{
    // Mirrors the game's language choices; no operating-system locale is consulted here.
    internal enum HudLanguage { English, French, Italian, German, Spanish, Russian, Brazilian, Polish, Chinese, Japanese, Korean, ChineseTraditional }

    internal sealed class SettingsMessage
    {
        private readonly string[] _translations;
        internal SettingsMessage(params string[] translations)
        {
            if (translations.Length != 12) throw new ArgumentException("Every message must contain all twelve game languages.");
            _translations = translations;
        }
        internal string Get(HudLanguage language)
        {
            int index = (int)language;
            return _translations[index >= 0 && index < _translations.Length ? index : 0];
        }
    }

    // Each message keeps all translations together, in HudLanguage order. Names and descriptions
    // are display metadata only; ConfigDefinition identities never come from this table.
    internal static class SettingsText
    {
        internal static readonly SettingsMessage General = new SettingsMessage(
            "General", "Général", "Generale", "Allgemein", "General", "Общие", "Geral", "Ogólne", "常规", "一般", "일반", "一般");
        internal static readonly SettingsMessage Recipes = new SettingsMessage(
            "Recipes", "Recettes", "Ricette", "Rezepte", "Recetas", "Рецепты", "Receitas", "Przepisy", "菜谱", "レシピ", "레시피", "食譜");
        internal static readonly SettingsMessage Contents = new SettingsMessage(
            "Container icons", "Icônes des récipients", "Icone dei contenitori", "Behältersymbole", "Iconos de recipientes", "Значки содержимого", "Ícones dos recipientes", "Ikony pojemników", "容器图标", "容器アイコン", "용기 아이콘", "容器圖示");
        internal static readonly SettingsMessage Opacity = new SettingsMessage(
            "Opacity", "Opacité", "Opacità", "Deckkraft", "Opacidad", "Непрозрачность", "Opacidade", "Nieprzezroczystość", "不透明度", "不透明度", "불투명도", "不透明度");

        internal static readonly SettingsMessage Enabled = new SettingsMessage(
            "Enable HUD customization", "Activer la personnalisation du HUD", "Attiva personalizzazione HUD", "HUD-Anpassung aktivieren", "Activar personalización del HUD", "Включить настройку интерфейса", "Ativar personalização do HUD", "Włącz dostosowanie HUD", "启用 HUD 自定义", "HUD カスタマイズを有効化", "HUD 사용자 설정 활성화", "啟用 HUD 自訂");
        internal static readonly SettingsMessage EnabledDescription = new SettingsMessage(
            "Customize this player's HUD. Disabling restores changes owned by this mod.",
            "Personnalise le HUD de ce joueur. Désactiver restaure les modifications appartenant à ce mod.",
            "Personalizza l'HUD di questo giocatore. Disattivando si ripristinano le modifiche gestite dalla mod.",
            "Passt das lokale HUD an. Beim Deaktivieren werden die Änderungen dieser Mod zurückgesetzt.",
            "Personaliza el HUD de este jugador. Al desactivar, se restauran los cambios controlados por este mod.",
            "Настраивает интерфейс этого игрока. Отключение отменяет изменения, которыми управляет этот мод.",
            "Personaliza o HUD deste jogador. Desativar restaura as alterações controladas por este mod.",
            "Dostosowuje HUD tego gracza. Wyłączenie przywraca zmiany kontrolowane przez ten mod.",
            "自定义本机玩家的 HUD；关闭后恢复本 MOD 仍拥有的显示改动。",
            "このプレイヤーの HUD を調整します。無効にすると、この MOD が管理する変更を元に戻します。",
            "이 플레이어의 HUD를 조정합니다. 비활성화하면 이 모드가 관리하는 변경 사항을 복원합니다.",
            "自訂本機玩家的 HUD；關閉後還原本 MOD 仍擁有的顯示變更。");

        internal static readonly SettingsMessage RecipeScale = new SettingsMessage(
            "Recipe scale", "Taille des recettes", "Dimensione ricette", "Rezeptgröße", "Tamaño de las recetas", "Масштаб рецептов", "Escala das receitas", "Skala przepisów", "菜谱大小", "レシピの大きさ", "레시피 크기", "食譜大小");
        internal static readonly SettingsMessage RecipeScaleDescription = new SettingsMessage(
            "Scale recipe cards from 0.5 to 2.0. 1 keeps each card's native scale.",
            "Taille des cartes de recette de 0,5 à 2,0. 1 conserve la taille d'origine.",
            "Scala le schede ricetta da 0,5 a 2,0. 1 mantiene la scala originale.",
            "Skaliert Rezeptkarten von 0,5 bis 2,0. 1 behält die Originalgröße bei.",
            "Escala las tarjetas de receta de 0,5 a 2,0. 1 conserva el tamaño original.",
            "Масштаб карточек рецептов от 0,5 до 2,0. 1 сохраняет исходный размер.",
            "Escala dos cartões de receita de 0,5 a 2,0. 1 mantém o tamanho original.",
            "Skala kart przepisów od 0,5 do 2,0. 1 zachowuje oryginalny rozmiar.",
            "菜谱卡片大小倍率，范围 0.5–2.0；1 保留每张卡片的原始大小。",
            "レシピカードの倍率（0.5～2.0）。1 で元の大きさを維持します。",
            "레시피 카드 크기 배율입니다(0.5~2.0). 1은 원래 크기를 유지합니다.",
            "食譜卡片大小倍率，範圍 0.5–2.0；1 保留每張卡片的原始大小。");

        internal static readonly SettingsMessage RecipesPerRow = new SettingsMessage(
            "Recipes per row", "Recettes par ligne", "Ricette per riga", "Rezepte pro Zeile", "Recetas por fila", "Рецептов в строке", "Receitas por linha", "Przepisy w wierszu", "每行菜谱上限", "1 行のレシピ数の上限", "행당 최대 레시피 수", "每行食譜上限");
        internal static readonly SettingsMessage RecipesPerRowDescription = new SettingsMessage(
            "Maximum cards per row (1–20). 0 keeps the native single row. Wide cards wrap earlier at the parent bounds; order count is unchanged.",
            "Maximum de cartes par ligne (1–20). 0 conserve la ligne d'origine. Les cartes larges passent à la ligne si nécessaire ; le nombre de commandes ne change pas.",
            "Massimo di schede per riga (1–20). 0 mantiene la riga originale. Le schede larghe vanno a capo prima del limite; il numero di ordini non cambia.",
            "Maximale Karten pro Zeile (1–20). 0 behält die Originalzeile bei. Breite Karten werden am Rand früher umgebrochen; die Auftragszahl bleibt gleich.",
            "Máximo de tarjetas por fila (1–20). 0 conserva la fila original. Las tarjetas anchas saltan de fila al llegar al límite; no cambia la cantidad de pedidos.",
            "Максимум карточек в строке (1–20). 0 сохраняет одну исходную строку. Широкие карточки переносятся у границы области; число заказов не меняется.",
            "Máximo de cartões por linha (1–20). 0 mantém a linha original. Cartões largos mudam de linha ao atingir o limite; a quantidade de pedidos não muda.",
            "Maksymalna liczba kart w wierszu (1–20). 0 zachowuje oryginalny jeden wiersz. Szerokie karty zawijają się przy granicy obszaru; liczba zamówień się nie zmienia.",
            "每行最多显示 1–20 张卡片；0 保留原版单行。宽菜谱到达父区域边界时提前换行，不改变订单总量。",
            "1 行のカード上限（1～20）。0 は元の 1 行表示です。幅が広いカードは領域の端で早めに折り返します。注文数は変わりません。",
            "행당 카드 수 상한입니다(1~20). 0은 원래 한 줄 배치입니다. 넓은 카드는 영역 경계에서 일찍 줄바꿈하며 주문 수는 바꾸지 않습니다.",
            "每行最多顯示 1–20 張卡片；0 保留原版單行。寬食譜到達父區域邊界時提前換行，不改變訂單總量。");

        internal static readonly SettingsMessage RecipeMaxRows = new SettingsMessage(
            "Maximum recipe rows", "Nombre max. de lignes", "Numero massimo di righe", "Maximale Rezeptzeilen", "Máximo de filas", "Макс. строк рецептов", "Máx. de linhas de receitas", "Limit wierszy przepisów", "菜谱最大行数", "レシピの最大行数", "최대 레시피 행 수", "食譜最大行數");
        internal static readonly SettingsMessage RecipeMaxRowsDescription = new SettingsMessage(
            "Caps recipe rows when wrapping is enabled. 0 means no limit. Extra orders continue to the right on the last row.",
            "Limite le nombre de lignes de recettes si le retour à la ligne est activé. 0 signifie sans limite. Les commandes en plus continuent à droite sur la dernière ligne.",
            "Limita il numero di righe se è attivo il ritorno a capo. 0 indica nessun limite. Gli ordini in più proseguono a destra sull'ultima riga.",
            "Begrenzt bei aktiviertem Zeilenumbruch die Anzahl der Zeilen. 0 bedeutet unbegrenzt. Weitere Bestellungen werden rechts an die letzte Zeile angehängt.",
            "Limita el número de filas cuando se habilita el salto de fila. 0 significa sin límite. Los pedidos adicionales se añaden a la derecha de la última fila.",
            "Ограничивает число строк при включённом переносе. 0 означает отсутствие ограничения. Остальные заказы добавляются справа в последней строке.",
            "Limita o número de linhas quando a quebra de linha está ativada. 0 significa sem limite. Os pedidos restantes continuam à direita na última linha.",
            "Ogranicza liczbę wierszy przy włączonym zawijaniu. 0 oznacza brak limitu. Kolejne zamówienia są dopisywane po prawej stronie ostatniego wiersza.",
            "启用换行时限制菜谱行数。0 表示不限。达到上限后，余下的订单按顺序接在末行右侧。",
            "折り返しが有効な場合にレシピの行数を制限します。0 は無制限です。残りの注文は最終行の右側に並びます。",
            "줄바꿈이 켜져 있을 때 레시피 행 수를 제한합니다. 0은 제한 없음입니다. 남은 주문은 마지막 행의 오른쪽에 이어집니다.",
            "啟用換行時限制食譜行數。0 表示不限。達到上限後，餘下的訂單依序接在末行右側。");

        internal static readonly SettingsMessage RecipeRowSpacing = new SettingsMessage(
            "Recipe row spacing", "Espacement des lignes", "Spaziatura righe ricette", "Rezept-Zeilenabstand", "Espacio entre filas", "Интервал строк рецептов", "Espaço entre linhas", "Odstęp między wierszami", "菜谱行间距", "レシピの行間", "레시피 행 간격", "食譜行距");
        internal static readonly SettingsMessage RecipeRowSpacingDescription = new SettingsMessage(
            "Gap between custom recipe rows, in Canvas units (0–100). Has no effect on the native single row.",
            "Espace entre les lignes personnalisées, en unités Canvas (0–100). Sans effet sur la ligne d'origine.",
            "Spazio tra le righe personalizzate, in unità Canvas (0–100). Nessun effetto sulla riga originale.",
            "Abstand zwischen angepassten Zeilen in Canvas-Einheiten (0–100). Ohne Wirkung bei der Originalzeile.",
            "Separación entre filas personalizadas, en unidades Canvas (0–100). No afecta a la fila original.",
            "Промежуток между строками в единицах Canvas (0–100). Не влияет на исходную одиночную строку.",
            "Espaço entre linhas personalizadas, em unidades Canvas (0–100). Não afeta a linha original.",
            "Odstęp między własnymi wierszami w jednostkach Canvas (0–100). Nie wpływa na oryginalny jeden wiersz.",
            "自定义多行菜谱的行间距，范围 0–100 Canvas 单位；原版单行模式不使用此项。",
            "カスタム行の間隔（Canvas 単位、0～100）。元の 1 行表示には影響しません。",
            "사용자 배치 레시피 행 사이 간격입니다(Canvas 단위, 0~100). 원래 한 줄 배치에는 영향이 없습니다.",
            "自訂多行食譜的行距，範圍 0–100 Canvas 單位；原版單行模式不使用此項。");

        internal static readonly SettingsMessage ContentsScale = new SettingsMessage(
            "Container icon scale", "Taille des icônes", "Dimensione icone contenitori", "Größe der Behältersymbole", "Tamaño de los iconos", "Масштаб значков содержимого", "Escala dos ícones", "Skala ikon pojemników", "容器图标大小", "容器アイコンの大きさ", "용기 아이콘 크기", "容器圖示大小");
        internal static readonly SettingsMessage ContentsScaleDescription = new SettingsMessage(
            "Shared scale for ingredient and empty-slot icons (0.5–2.0). 1 preserves native size. Independent of column count and capacity.",
            "Taille commune aux ingrédients et emplacements vides (0,5–2,0). 1 conserve la taille d'origine. Indépendante des colonnes et de la capacité.",
            "Scala condivisa per ingredienti e spazi vuoti (0,5–2,0). 1 mantiene la dimensione originale. Indipendente da colonne e capacità.",
            "Gemeinsame Größe für Zutaten und leere Plätze (0,5–2,0). 1 behält die Originalgröße bei. Unabhängig von Spaltenzahl und Kapazität.",
            "Escala común de ingredientes y huecos vacíos (0,5–2,0). 1 conserva el tamaño original. Independiente de columnas y capacidad.",
            "Общий масштаб значков ингредиентов и пустых мест (0,5–2,0). 1 сохраняет исходный размер. Не зависит от числа столбцов и вместимости.",
            "Escala compartilhada de ingredientes e espaços vazios (0,5–2,0). 1 mantém o tamanho original. Independente de colunas e capacidade.",
            "Wspólna skala ikon składników i pustych miejsc (0,5–2,0). 1 zachowuje oryginalny rozmiar. Niezależna od kolumn i pojemności.",
            "食材与空位图标共用的大小倍率，范围 0.5–2.0；1 保留原始大小。独立于每行数量，不改变容量。",
            "食材と空き枠アイコン共通の倍率（0.5～2.0）。1 は元の大きさです。列数とは独立しており、容量は変わりません。",
            "식재료와 빈 슬롯 아이콘의 공통 크기 배율입니다(0.5~2.0). 1은 원래 크기입니다. 열 수와 독립적이며 용량은 바꾸지 않습니다.",
            "食材與空位圖示共用的大小倍率，範圍 0.5–2.0；1 保留原始大小。獨立於每行數量，不改變容量。");

        internal static readonly SettingsMessage ContentsPerRow = new SettingsMessage(
            "Container icons per row", "Icônes par ligne", "Icone per riga", "Behältersymbole pro Zeile", "Iconos por fila", "Значков в строке", "Ícones por linha", "Ikony w wierszu", "每行容器图标上限", "1 行の容器アイコン数の上限", "행당 최대 용기 아이콘 수", "每行容器圖示上限");
        internal static readonly SettingsMessage ContentsPerRowDescription = new SettingsMessage(
            "Maximum icons per row (1–20). 0 preserves the container's native layout. More columns expand the display without shrinking icons or changing capacity.",
            "Maximum d'icônes par ligne (1–20). 0 conserve la disposition d'origine. Les colonnes élargissent l'affichage sans réduire les icônes ni changer la capacité.",
            "Massimo di icone per riga (1–20). 0 mantiene il layout originale. Più colonne ampliano l'area senza ridurre le icone o modificare la capacità.",
            "Maximale Symbole pro Zeile (1–20). 0 behält das Original-Layout bei. Mehr Spalten verbreitern die Anzeige, ohne Symbole zu verkleinern oder die Kapazität zu ändern.",
            "Máximo de iconos por fila (1–20). 0 conserva la distribución original. Más columnas amplían el área sin reducir los iconos ni cambiar la capacidad.",
            "Максимум значков в строке (1–20). 0 сохраняет исходную раскладку. Дополнительные столбцы расширяют область без уменьшения значков или изменения вместимости.",
            "Máximo de ícones por linha (1–20). 0 mantém o layout original. Mais colunas ampliam a área sem reduzir ícones nem alterar a capacidade.",
            "Maksymalna liczba ikon w wierszu (1–20). 0 zachowuje oryginalny układ. Więcej kolumn poszerza obszar bez zmniejszania ikon ani zmiany pojemności.",
            "每行最多显示 1–20 个图标；0 保留容器原布局。增加列数会扩宽显示区域，不缩小图标，也不改变容量。",
            "1 行のアイコン上限（1～20）。0 は元の配置です。列を増やすと表示領域が広がります。アイコンの大きさや容量は変わりません。",
            "행당 아이콘 수 상한입니다(1~20). 0은 원래 배치입니다. 열을 늘리면 표시 영역이 넓어지며 아이콘 크기나 용량은 바꾸지 않습니다.",
            "每行最多顯示 1–20 個圖示；0 保留容器原布局。增加欄數會擴寬顯示區域，不縮小圖示，也不改變容量。");

        internal static readonly SettingsMessage RecipeOpacity = new SettingsMessage(
            "Recipe opacity (%)", "Opacité des recettes (%)", "Opacità ricette (%)", "Rezept-Deckkraft (%)", "Opacidad de recetas (%)", "Непрозрачность рецептов (%)", "Opacidade das receitas (%)", "Nieprzezroczystość przepisów (%)", "菜谱不透明度（%）", "レシピの不透明度（%）", "레시피 불투명도 (%)", "食譜不透明度（%）");
        internal static readonly SettingsMessage RecipeOpacityDescription = new SettingsMessage(
            "Opacity multiplier for the entire recipe card (0–100%). 100 preserves native opacity; 0 hides it. Native fade animations remain active.",
            "Multiplicateur d'opacité de toute la carte (0–100 %). 100 conserve l'opacité d'origine ; 0 la rend invisible. Les fondus d'origine restent actifs.",
            "Moltiplicatore di opacità dell'intera scheda (0–100%). 100 mantiene l'opacità originale; 0 la nasconde. Le dissolvenze originali restano attive.",
            "Deckkraftfaktor der gesamten Rezeptkarte (0–100 %). 100 behält die Originaldeckkraft bei; 0 blendet sie aus. Originale Ausblendanimationen bleiben aktiv.",
            "Multiplicador de opacidad de toda la tarjeta (0–100 %). 100 conserva la opacidad original; 0 la oculta. Se mantienen los fundidos originales.",
            "Множитель непрозрачности всей карточки (0–100%). 100 сохраняет исходную непрозрачность; 0 скрывает её. Исходные анимации затухания сохраняются.",
            "Multiplicador de opacidade do cartão inteiro (0–100%). 100 mantém a opacidade original; 0 o oculta. As animações originais de fade continuam ativas.",
            "Mnożnik nieprzezroczystości całej karty (0–100%). 100 zachowuje oryginalny wygląd; 0 ją ukrywa. Oryginalne animacje zanikania nadal działają.",
            "整张菜谱卡片的不透明度倍率（0–100%）；100 保留原生不透明度，0 完全透明，原生淡出动画仍继续。",
            "レシピカード全体の不透明度倍率（0～100%）。100 は元の表示、0 は透明です。元のフェードアニメーションも維持します。",
            "레시피 카드 전체의 불투명도 배율입니다(0~100%). 100은 원래 표시, 0은 완전 투명입니다. 원래 페이드 애니메이션은 유지됩니다.",
            "整張食譜卡片的不透明度倍率（0–100%）；100 保留原生不透明度，0 完全透明，原生淡出動畫仍繼續。");

        internal static readonly SettingsMessage EmptySlotOpacity = new SettingsMessage(
            "Empty-slot opacity (%)", "Opacité des emplacements vides (%)", "Opacità spazi vuoti (%)", "Deckkraft leerer Plätze (%)", "Opacidad de huecos vacíos (%)", "Непрозрачность пустых мест (%)", "Opacidade dos espaços vazios (%)", "Nieprzezroczystość pustych miejsc (%)", "空位加号不透明度（%）", "空き枠の不透明度（%）", "빈 슬롯 불투명도 (%)", "空位加號不透明度（%）");
        internal static readonly SettingsMessage EmptySlotOpacityDescription = new SettingsMessage(
            "Opacity multiplier for empty-slot icons (0–100%). 100 preserves native opacity; 0 hides them. Slots keep their positions and capacity.",
            "Multiplicateur d'opacité des emplacements vides (0–100 %). 100 conserve l'opacité d'origine ; 0 les masque. Positions et capacité restent inchangées.",
            "Moltiplicatore di opacità degli spazi vuoti (0–100%). 100 mantiene l'opacità originale; 0 li nasconde. Posizioni e capacità restano invariate.",
            "Deckkraftfaktor leerer Plätze (0–100 %). 100 behält die Originaldeckkraft bei; 0 blendet sie aus. Positionen und Kapazität bleiben gleich.",
            "Multiplicador de opacidad de huecos vacíos (0–100 %). 100 conserva la opacidad original; 0 los oculta. Se mantienen las posiciones y la capacidad.",
            "Множитель непрозрачности пустых мест (0–100%). 100 сохраняет исходный вид; 0 скрывает значки. Позиции и вместимость не меняются.",
            "Multiplicador de opacidade dos espaços vazios (0–100%). 100 mantém a opacidade original; 0 os oculta. Posições e capacidade não mudam.",
            "Mnożnik nieprzezroczystości pustych miejsc (0–100%). 100 zachowuje oryginalny wygląd; 0 je ukrywa. Pozycje i pojemność się nie zmieniają.",
            "容器空位加号的不透明度倍率（0–100%）；100 保留原生显示，0 完全透明。透明空位仍占原位置，容量不变。",
            "空き枠アイコンの不透明度倍率（0～100%）。100 は元の表示、0 は透明です。枠の位置と容量は変わりません。",
            "빈 슬롯 아이콘의 불투명도 배율입니다(0~100%). 100은 원래 표시, 0은 완전 투명입니다. 슬롯 위치와 용량은 유지됩니다.",
            "容器空位加號的不透明度倍率（0–100%）；100 保留原生顯示，0 完全透明。透明空位仍佔原位置，容量不變。");

        internal static readonly SettingsMessage IngredientOpacity = new SettingsMessage(
            "Ingredient opacity (%)", "Opacité des ingrédients (%)", "Opacità ingredienti (%)", "Zutaten-Deckkraft (%)", "Opacidad de ingredientes (%)", "Непрозрачность ингредиентов (%)", "Opacidade dos ingredientes (%)", "Nieprzezroczystość składników (%)", "食材不透明度（%）", "食材の不透明度（%）", "식재료 불투명도 (%)", "食材不透明度（%）");
        internal static readonly SettingsMessage IngredientOpacityDescription = new SettingsMessage(
            "Opacity multiplier for contents and burnt/overmixed status icons (0–100%). 100 preserves native opacity; 0 hides them. Does not affect empty slots or gameplay.",
            "Multiplicateur d'opacité du contenu et des états brûlé/trop mélangé (0–100 %). 100 conserve l'opacité d'origine ; 0 les masque. Sans effet sur les emplacements vides ou le jeu.",
            "Moltiplicatore di opacità del contenuto e degli stati bruciato/troppo mescolato (0–100%). 100 mantiene l'opacità originale; 0 li nasconde. Non modifica spazi vuoti o gioco.",
            "Deckkraftfaktor für Inhalte und Verbrannt-/Übermixt-Symbole (0–100 %). 100 behält die Originaldeckkraft bei; 0 blendet sie aus. Leere Plätze und Spielablauf bleiben unverändert.",
            "Multiplicador de opacidad del contenido y estados quemado/mezclado en exceso (0–100 %). 100 conserva la opacidad original; 0 los oculta. No afecta a huecos vacíos ni a la partida.",
            "Множитель непрозрачности содержимого и значков подгорания/избыточного смешивания (0–100%). 100 сохраняет исходный вид; 0 скрывает их. Не влияет на пустые места и игровой процесс.",
            "Multiplicador de opacidade do conteúdo e dos estados queimado/misturado demais (0–100%). 100 mantém a opacidade original; 0 os oculta. Não afeta espaços vazios ou a partida.",
            "Mnożnik nieprzezroczystości zawartości oraz ikon przypalenia/nadmiernego mieszania (0–100%). 100 zachowuje oryginalny wygląd; 0 je ukrywa. Nie wpływa na puste miejsca ani rozgrywkę.",
            "真实内容及烧焦、过搅拌状态图标的不透明度倍率（0–100%）；100 保留原生显示，0 完全透明。不影响空位图标和游戏逻辑。",
            "内容物と焦げ・混ぜすぎ状態アイコンの不透明度倍率（0～100%）。100 は元の表示、0 は透明です。空き枠やゲーム処理には影響しません。",
            "내용물과 탄 음식·과도한 혼합 상태 아이콘의 불투명도 배율입니다(0~100%). 100은 원래 표시, 0은 완전 투명입니다. 빈 슬롯이나 게임 동작에는 영향이 없습니다.",
            "真實內容及燒焦、過度攪拌狀態圖示的不透明度倍率（0–100%）；100 保留原生顯示，0 完全透明。不影響空位圖示和遊戲邏輯。");
    }
}
