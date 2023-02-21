# Missing Reference Finder
Утилита, позволяющая находить отсутствующие (missing) ссылки в ассетах проекта

## Установка ##
Загрузите MissingReferenceFinder.unitypackage, и установите его в проект.

## Запуск и использование ##
Находясь в редакторе откройте меню Tools -> Missing References Finder, в открывшемся окне нажмите на кнопку "Search for Missing References", после чего запустится поиск.
После окончания поиска в левом столбце будут располагаться все ассеты, в которых присутствует хотя бы 1 отсутствующая ссылка. По нажатию на ассет в правом столбце появится список всех отсутствующих ссылок внутри этого компонента, а также ассет покажется в окне Project.

## Принцип работы ##
После нажатия на кнопку получаем список guid всех сцен и всех префабов, находящихся в проекте в папке Assets. Поочередно загружаем сцену, получаем все объекты, находящиеся в ней, и проверяем на отсутствующие ссылки. Как только все сцены были проверены, переходим к проверке всех префабов в проекте. Поиск missing ссылок происходит путем проверки всех SerializedProperty у каждого компонента внутри GameObject'a. Если ID ссылки на компонент не нулевой (т.е. ссылка была установлена), а сама ссылка отсутствует, то это отсутствующая ссылка. 

## Другие способы реализации ##
Нужно также получить guid всех ассетов, находящихся в проекте, со затем в цикле проходим по каждому ассету и проверяем все guid, на которые ссылается данный ассет. Если guid не присутствует в проекте, значит это missing ссылка. Был выбран предыдущий метод, т.к. он проще в реализации.

## Скриншоты ##
