# PE Dependency Analyzer

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-windows-lightgrey.svg)](https://github.com/yourusername/PEDependencyAnalyzer)
[![ru](https://img.shields.io/badge/lang-ru-red.svg)](https://github.com/yourusername/PEDependencyAnalyzer/blob/main/README.ru.md)

Topics: `dotnet`, `windows`, `pe`, `binary-analysis`, `dependency-analysis`, `native`

Консольная утилита для анализа и публикации зависимостей Windows PE файлов (исполняемых файлов и DLL).

## Возможности

- Анализ зависимостей PE файлов (как нативных, так и управляемых)
- Классификация зависимостей по категориям:
  - Системные зависимости (системные DLL Windows)
  - Runtime-зависимости (библиотеки MSVC, MFC и .NET runtime)
  - Виртуальные зависимости (Windows API Sets)
  - Прочие зависимости (DLL приложения)
- Поддержка рекурсивного анализа зависимостей
- Возможность публикации приложения вместе с зависимостями

## Требования

- .NET 8.0 или выше
- Операционная система Windows

## Установка

1. Клонируйте репозиторий
2. Соберите проект:
```bash
dotnet build --configuration Release
```

## Варианты сборки

Анализатор может быть собран несколькими способами в зависимости от ваших потребностей:

### 1. Нативная AOT-сборка (Максимальная производительность)
- Самый быстрый запуск
- Лучшая производительность во время выполнения
- Наименьшее потребление памяти
- Зависит от платформы (требуются отдельные сборки для разных платформ)
- Ограниченные возможности reflection
```bash
dotnet publish --configuration Release /p:PublishProfile=NativeAot
```
Результат: `bin/Release/publish-native/PEDependencyAnalyzer.exe`

### 2. Сборка в один файл (Простое распространение)
- Единый исполняемый файл
- Включает все зависимости
- Не требует установки
- Больший размер файла
- Немного более медленный запуск
```bash
dotnet publish --configuration Release /p:PublishProfile=SingleFile
```
Результат: `bin/Release/publish-single-file/PEDependencyAnalyzer.exe`

### 3. Зависимая от фреймворка сборка (Максимальная совместимость)
- Требует установленный .NET Runtime
- Наименьший размер дистрибутива
- Полные возможности времени выполнения
- Независимость от платформы
- Лучший вариант для разработки
```bash
dotnet publish --configuration Release /p:PublishProfile=Framework
```
Результат: `bin/Release/publish-framework/PEDependencyAnalyzer.exe`

### Сборка всех вариантов
Вы можете собрать все варианты сразу, используя предоставленный пакетный файл:
```bash
build-all.bat
```

Выберите подходящий вариант сборки в зависимости от ваших требований:
- Используйте Native AOT для лучшей производительности в продакшене
- Используйте Single-file для простого распространения конечным пользователям
- Используйте Framework-dependent для разработки или когда .NET Runtime уже установлен

## Использование

Базовый анализ:
```bash
PEDependencyAnalyzer <путь_к_файлу_или_директории>
```

Публикация с зависимостями:
```bash
PEDependencyAnalyzer <путь_к_файлу> --publish[=имя_директории]
```

### Параметры командной строки

- `--publish[=имя_директории]` - Включить режим публикации (директория по умолчанию: 'publish')
- `--no-runtime` - Исключить runtime DLL из публикации
- `--no-virtual` - Исключить виртуальные DLL из публикации

### Примеры

Анализ одного файла:
```bash
PEDependencyAnalyzer app.exe
```

Анализ всех исполняемых файлов в директории:
```bash
PEDependencyAnalyzer C:\MyApp
```

Публикация со всеми зависимостями:
```bash
PEDependencyAnalyzer app.exe --publish
```

Публикация в определенную директорию без runtime DLL:
```bash
PEDependencyAnalyzer app.exe --publish=dist --no-runtime
```

## Классификация зависимостей

Утилита классифицирует зависимости в следующем порядке:

1. **Виртуальные зависимости**: DLL, начинающиеся с "api-ms-win-" или "ext-ms-win-"
2. **Runtime-зависимости**: библиотеки MSVC runtime, MFC и .NET runtime
3. **Системные зависимости**: DLL, расположенные в системных директориях Windows
4. **Прочие зависимости**: все остальные DLL

## Пример вывода

```
System Dependencies:
-------------------
KERNEL32.dll                             C:\Windows\system32\KERNEL32.dll
USER32.dll                               C:\Windows\system32\USER32.dll
...

Runtime Dependencies:
--------------------
msvcrt.dll                               C:\Windows\system32\msvcrt.dll
msvcp_win.dll                            C:\Windows\system32\msvcp_win.dll
...

Virtual Dependencies (API Sets):
------------------------------
api-ms-win-core-console-l1-1-0.dll       ...
api-ms-win-core-debug-l1-1-0.dll         ...
...

Other Dependencies:
------------------
MyApp.Core.dll                           C:\MyApp\MyApp.Core.dll
...

Summary:
--------
System Dependencies: 17
Runtime Dependencies: 2
Virtual Dependencies: 35
Other Dependencies: 1
Total Dependencies: 55
```

## Лицензия

MIT License
