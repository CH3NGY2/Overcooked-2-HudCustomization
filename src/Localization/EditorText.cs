namespace OC2HudCustomization
{
    internal static class EditorText
    {
        internal static readonly SettingsMessage SelectCategoryText = new SettingsMessage("Select a category", "Choisir une catégorie", "Scegli categoria", "Kategorie wählen", "Elegir categoría", "Выберите категорию", "Escolher categoria", "Wybierz kategorię", "选择要编辑的类别", "種類を選択", "종류 선택", "選擇要編輯的類別");
        internal static readonly SettingsMessage CategoryActive = new SettingsMessage("Highlighted · preview enabled · click again to finish", "Sélection · aperçu actif · recliquer pour terminer", "Evidenziato · anteprima attiva · riclicca per finire", "Markiert · Vorschau aktiv · erneut klicken zum Beenden", "Resaltado · vista previa activa · pulsa de nuevo para terminar", "Выделено · просмотр включён · нажмите снова для завершения", "Destacado · prévia ativa · clique de novo para terminar", "Wyróżniono · podgląd aktywny · kliknij ponownie, aby zakończyć", "已高亮 · 预览已开启 · 再点此类别结束预览", "強調・プレビュー中。もう一度選ぶと終了", "강조·미리보기 켜짐 · 다시 클릭하면 종료", "已高亮 · 預覽已開啟 · 再點此類別結束預覽");
        internal static readonly SettingsMessage NoInstance = new SettingsMessage("Unavailable: no display created in this kitchen.", "Indisponible : aucun affichage dans cette cuisine.", "Non disponibile: nessuna istanza in cucina.", "Nicht verfügbar: keine Anzeige in dieser Küche.", "No disponible: sin elemento en esta cocina.", "Недоступно: в этой кухне элемент не создан.", "Indisponível: nenhuma instância nesta cozinha.", "Niedostępne: brak elementu w tej kuchni.", "本关尚未创建此类界面，暂不可选。", "この厨房では未生成のため選択できません。", "이 주방에 생성된 표시가 없어 선택할 수 없습니다.", "本關尚未建立此類介面，暫不可選。");
        internal static readonly SettingsMessage EmptySlotOpacity = new SettingsMessage("Plus opacity (%)", "Opacité des + (%)", "Opacità + (%)", "Plus-Deckkraft (%)", "Opacidad de + (%)", "Непрозрачность + (%)", "Opacidade de + (%)", "Nieprzezroczystość + (%)", "加号不透明度（%）", "+ の不透明度（%）", "+ 불투명도 (%)", "加號不透明度（%）");
        internal static readonly SettingsMessage IngredientOpacity = new SettingsMessage("Ingredient opacity (%)", "Opacité des ingrédients (%)", "Opacità ingredienti (%)", "Zutaten-Deckkraft (%)", "Opacidad de ingredientes (%)", "Непрозрачность ингредиентов (%)", "Opacidade dos ingredientes (%)", "Nieprzezroczystość składników (%)", "原材料不透明度（%）", "食材の不透明度（%）", "식재료 불투명도 (%)", "原材料不透明度（%）");
        internal static readonly SettingsMessage LiveLayout = new SettingsMessage("Live preview · 0 per row keeps native layout", "Aperçu direct · 0 par ligne conserve le natif", "Anteprima · 0 per riga mantiene layout nativo", "Live-Vorschau · 0 pro Reihe behält Original", "Vista previa · 0 por fila usa disposición nativa", "Просмотр · 0 в строке сохраняет исходную раскладку", "Prévia · 0 por linha mantém o layout nativo", "Podgląd · 0 w wierszu zachowuje układ gry", "实时预览 · 每行数量设为 0 时使用原生布局", "即時プレビュー · 一行 0 は元の配置", "실시간 미리보기 · 행당 0은 기본 배치", "即時預覽 · 每行數量設為 0 時使用原生排版");
        internal static readonly SettingsMessage Save = new SettingsMessage("Save & exit (`)", "Enregistrer (`)", "Salva ed esci (`)", "Speichern & Ende (`)", "Guardar y salir (`)", "Сохранить и выйти (`)", "Salvar e sair (`)", "Zapisz i zamknij (`)", "保存并退出 (`)", "保存して終了 (`)", "저장 후 종료 (`)", "儲存並退出 (`)");
        internal static readonly SettingsMessage Cancel = new SettingsMessage("Cancel changes", "Annuler", "Annulla", "Verwerfen", "Cancelar", "Отмена", "Cancelar", "Anuluj zmiany", "取消修改", "変更を取消", "변경 취소", "取消修改");
        // English, French, Italian, German, Spanish, Russian, Brazilian, Polish, Chinese, Japanese, Korean, Traditional Chinese.
        internal static readonly SettingsMessage Enter = new SettingsMessage("Enter HUD editor", "Modifier le HUD", "Modifica HUD", "HUD bearbeiten", "Editar HUD", "Редактор интерфейса", "Editar HUD", "Edytuj HUD", "进入 HUD 编辑模式", "HUD 編集モード", "HUD 편집 모드", "進入 HUD 編輯模式");
        internal static readonly SettingsMessage EntryDescription = new SettingsMessage(
            "Local kitchen only (no remote players). Pauses the game; press ` to save and exit. Enable this mod first.",
            "Cuisine locale sans joueurs distants. Met en pause ; ` pour enregistrer et quitter. Activez le mod.",
            "Solo cucina locale senza giocatori remoti. Pausa il gioco; ` salva ed esce. Attiva la mod.",
            "Nur lokale Küche ohne entfernte Spieler. Pausiert; ` speichert und beendet. Mod aktivieren.",
            "Solo cocina local sin jugadores remotos. Pausa; ` guarda y sale. Activa el mod.",
            "Только локальная кухня без удалённых игроков. Пауза; ` — сохранить и выйти. Включите мод.",
            "Apenas cozinha local sem jogadores remotos. Pausa; ` salva e sai. Ative o mod.",
            "Tylko lokalna kuchnia bez graczy zdalnych. Pauza; ` zapisuje i zamyka. Włącz mod.",
            "仅限无远程玩家的本地厨房。冻结画面，按 ` 保存退出；需先启用本 MOD。",
            "リモートプレイヤーのいないローカル厨房専用。一時停止し、` で保存して終了。MOD を有効にしてください。",
            "원격 플레이어가 없는 로컬 주방 전용. 일시 정지 후 ` 키로 저장하고 종료합니다. MOD를 켜세요.",
            "僅限無遠端玩家的本地廚房。凍結畫面，按 ` 儲存退出；需先啟用本 MOD。");
        internal static readonly SettingsMessage Title = new SettingsMessage("HUD editor — paused", "Éditeur HUD — pause", "Editor HUD — pausa", "HUD-Editor — pausiert", "Editor HUD — pausa", "Редактор HUD — пауза", "Editor HUD — pausado", "Edytor HUD — pauza", "HUD 编辑器 · 已暂停", "HUD 編集 · 一時停止", "HUD 편집 · 일시 정지", "HUD 編輯器 · 已暫停");
        internal static readonly SettingsMessage Help = new SettingsMessage(
            "Click to select; drag body/edges/corners. ` saves and exits. Changes affect the whole category.",
            "Cliquez ; glissez le centre, les bords ou coins. ` enregistre et quitte. Toute la catégorie change.",
            "Seleziona e trascina corpo, bordi o angoli. ` salva ed esce. Modifica tutta la categoria.",
            "Auswählen; Mitte, Kanten oder Ecken ziehen. ` speichert und beendet. Gilt für die Kategorie.",
            "Selecciona; arrastra cuerpo, bordes o esquinas. ` guarda y sale. Afecta a toda la categoría.",
            "Выберите и тяните центр, края или углы. ` — сохранить и выйти. Для всей категории.",
            "Selecione e arraste centro, bordas ou cantos. ` salva e sai. Afeta toda a categoria.",
            "Wybierz i przeciągnij środek, krawędzie lub rogi. ` zapisuje i zamyka. Cała kategoria.",
            "点击选中，拖主体移动、边缘改宽高、四角等比缩放。按 ` 保存退出；同类全局生效。",
            "クリックで選択。本体・辺・角をドラッグ。` で保存して終了。同じ種類すべてに適用。",
            "클릭하여 선택 후 본체·가장자리·모서리를 드래그하세요. ` 저장 및 종료. 같은 종류 전체 적용.",
            "點擊選取，拖主體移動、邊緣改寬高、四角等比縮放。按 ` 儲存退出；同類全域生效。");
        internal static readonly SettingsMessage Whitelist = new SettingsMessage("Editable categories", "Catégories modifiables", "Categorie modificabili", "Bearbeitbare Kategorien", "Categorías editables", "Доступные категории", "Categorias editáveis", "Edytowalne kategorie", "可编辑白名单", "編集できる種類", "편집 허용 목록", "可編輯白名單");
        internal static readonly SettingsMessage Preview = new SettingsMessage("Preview", "Aperçu", "Mostra", "Vorschau", "Ver", "Показать", "Prévia", "Podgląd", "预览", "表示", "미리보기", "預覽");
        internal static readonly SettingsMessage PreviewHelp = new SettingsMessage(
            "Temporarily show existing hidden displays while editing. Restored on exit. Unavailable if the level has not created an instance.",
            "Affiche les éléments masqués existants pendant l'édition. Rétablis en quittant. Indisponible sans instance.",
            "Mostra gli elementi nascosti esistenti durante la modifica. Ripristinati all'uscita. Richiede un'istanza nel livello.",
            "Vorhandene versteckte Anzeigen beim Bearbeiten einblenden. Beim Beenden wiederherstellen. Benötigt eine Instanz.",
            "Muestra elementos ocultos existentes al editar. Se restauran al salir. Requiere una instancia en el nivel.",
            "Временно показать существующие скрытые элементы. При выходе восстановить. Нужен созданный элемент уровня.",
            "Mostra elementos ocultos existentes ao editar. Restaura ao sair. Requer instância criada na fase.",
            "Pokaż istniejące ukryte elementy podczas edycji. Przywróć przy wyjściu. Wymaga instancji na poziomie.",
            "编辑时临时显示已创建的隐藏元素，退出后恢复；本关尚未创建实例时不可用。",
            "既存の非表示項目を編集時だけ表示。終了時に復元。ステージに未生成の場合は使用できません。",
            "편집 중 생성된 숨김 요소를 잠시 표시하고 종료 시 복원합니다. 레벨에 생성된 항목이 필요합니다.",
            "編輯時暫時顯示已建立的隱藏元素，退出後恢復；本關尚未建立實例時不可用。");
        internal static readonly SettingsMessage Select = new SettingsMessage("Click an outlined HUD element, or a category name to edit/reset hidden or offscreen elements.", "Cliquez sur un HUD encadré, ou une catégorie pour modifier/rétablir les éléments masqués ou hors écran.", "Seleziona un HUD con bordo, o una categoria per modificare/ripristinare elementi nascosti o fuori schermo.", "HUD-Rahmen oder Kategorienamen anklicken; auch unsichtbare Elemente lassen sich bearbeiten/zurücksetzen.", "Elige un HUD con borde o una categoría para editar/restablecer elementos ocultos o fuera de pantalla.", "Выберите HUD с рамкой или категорию для настройки/сброса скрытых элементов и элементов за экраном.", "Selecione um HUD com contorno ou uma categoria para editar/redefinir itens ocultos ou fora da tela.", "Wybierz HUD z ramką lub kategorię, aby edytować/resetować elementy ukryte i poza ekranem.", "点击带边框的 HUD，或点击类别名称调整、重置已隐藏或移出屏幕的元素。", "枠のある HUD か種類名を選択。非表示・画面外の項目も編集・リセットできます。", "테두리 HUD 또는 종류 이름을 선택하세요. 숨겨지거나 화면 밖인 항목도 수정·초기화할 수 있습니다.", "點擊帶邊框的 HUD，或點擊類別名稱調整、重設已隱藏或移出畫面的元素。");
        internal static readonly SettingsMessage Scale = new SettingsMessage("Scale", "Échelle", "Scala", "Skalierung", "Escala", "Масштаб", "Escala", "Skala", "大小倍率", "拡大率", "크기", "大小倍率");
        internal static readonly SettingsMessage Width = new SettingsMessage("Width", "Largeur", "Larghezza", "Breite", "Ancho", "Ширина", "Largura", "Szerokość", "宽度倍率", "幅", "너비", "寬度倍率");
        internal static readonly SettingsMessage Height = new SettingsMessage("Height", "Hauteur", "Altezza", "Höhe", "Alto", "Высота", "Altura", "Wysokość", "高度倍率", "高さ", "높이", "高度倍率");
        internal static readonly SettingsMessage Opacity = new SettingsMessage("Opacity (%)", "Opacité (%)", "Opacità (%)", "Deckkraft (%)", "Opacidad (%)", "Непрозрачность (%)", "Opacidade (%)", "Nieprzezr. (%)", "不透明度 (%)", "不透明度 (%)", "불투명도 (%)", "不透明度 (%)");
        internal static readonly SettingsMessage Reset = new SettingsMessage("Reset this category", "Réinitialiser la catégorie", "Ripristina categoria", "Kategorie zurücksetzen", "Restablecer categoría", "Сбросить категорию", "Redefinir categoria", "Resetuj kategorię", "恢复当前类别默认值", "この種類をリセット", "이 종류 초기화", "恢復目前類別預設值");
        internal static readonly SettingsMessage SaveError = new SettingsMessage("Save failed or invalid number. Correct and press ` again.", "Échec ou nombre invalide. Corrigez puis `.", "Errore o numero non valido. Correggi e premi `.", "Speicherfehler oder ungültige Zahl. Korrigieren, dann `.", "Error o número inválido. Corrige y pulsa `.", "Ошибка записи или числа. Исправьте и нажмите `.", "Falha ou número inválido. Corrija e pressione `.", "Błąd zapisu lub liczby. Popraw i naciśnij `.", "保存失败或数字无效，请修正后再次按 `。", "保存失敗または無効な数値。修正後に ` を押してください。", "저장 실패 또는 잘못된 숫자. 수정 후 ` 키를 누르세요.", "儲存失敗或數字無效，請修正後再次按 `。");
        internal static readonly SettingsMessage[] Kinds = new SettingsMessage[] {
            new SettingsMessage("Scoreboard", "Score", "Punteggio", "Punktestand", "Marcador", "Счёт", "Placar", "Wynik", "计分板", "スコア", "점수판", "計分板"),
            new SettingsMessage("Timer", "Chronomètre", "Timer", "Timer", "Temporizador", "Таймер", "Cronômetro", "Czas", "倒计时", "タイマー", "타이머", "倒數計時"),
            new SettingsMessage("Chopping", "Découpe", "Taglio", "Schneiden", "Cortar", "Нарезка", "Corte", "Krojenie", "切菜进度", "切る進捗", "썰기", "切菜進度"),
            new SettingsMessage("Cooking / skewers", "Cuisson / brochettes", "Cottura / spiedini", "Kochen / Spieße", "Cocción / brochetas", "Готовка / шашлыки", "Cozimento / espetos", "Gotowanie / szaszłyki", "烹饪 / 烤串进度", "調理 / 串焼き", "요리 / 꼬치", "烹飪 / 烤串進度"),
            new SettingsMessage("Mixing", "Mélange", "Miscelazione", "Mixen", "Mezclar", "Смешивание", "Mistura", "Mieszanie", "搅拌进度", "混ぜる進捗", "섞기", "攪拌進度"),
            new SettingsMessage("BBQ heat", "Chaleur du barbecue", "Calore barbecue", "Grillhitze", "Calor de barbacoa", "Жар гриля", "Calor da grelha", "Żar grilla", "烧烤火力 / 热力条", "バーベキュー火力", "바비큐 화력", "燒烤火力 / 熱力條"),
            new SettingsMessage("Washing", "Lavage", "Lavaggio", "Spülen", "Lavar", "Мытьё", "Lavagem", "Zmywanie", "洗碗进度", "洗う進捗", "설거지", "洗碗進度"),
            new SettingsMessage("Extinguishing", "Extinction", "Estinzione", "Löschen", "Extinción", "Тушение", "Extinção", "Gaszenie", "灭火进度", "消火進捗", "소화", "滅火進度")
        };
    }
}
