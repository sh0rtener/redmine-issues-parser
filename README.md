# Redmine Issues Parser

Сервис для парсинга, обработки и хранения задач из Redmine. Предоставляет Web API для получения структурированных данных и сохраняет их в PostgreSQL.

## Алгоритм работы
1. Инициализация: сервис считывает конфигурацию (URL Redmine, API-ключ, строка подключения к БД) из переменных окружения.
2. Получение данных: выполняет авторизованные HTTP-запросы Basic авторизации к Redmine REST API для получения списка задач (issues) по заданным фильтрам.
3. Обработка: парсит XML-ответ, маппит данные во внутренние модели, применяет бизнес-логику (фильтрация, очистка, преобразование форматов).
4. Сохранение: записывает или обновляет (upsert) обработанные данные в таблице PostgreSQL.

## Стек технологий
- Язык: C# (.NET 8) 
- Архитектура: Worker
- База данных: PostgreSQL
- Контейнеризация: Docker, Docker Compose

## Структура проекта

├── .github/workflows/       # CI/CD пайплайны (тесты, сборка образа) <br/>
├── infra/                   # Инфраструктура (docker-compose, .env.example, SQL-скрипты) <br/>
├── src/ <br/>
│   ├── RedmineParser.WebApi # Основной проект API <br/>
│   ├── RedmineParser.Tests  # Юнит-тесты <br/>
│   └── RedmineParser.slnx   # Файл решения <br/>
└── README.md

## Запуск через Docker Compose (рекомендуется)
1. Скопируйте файл окружения:
   cp infra/.env.example infra/.env
2. Отредактируйте `infra/.env`, указав реальные данные (Redmine URL, API-ключ, параметры БД).
3. Запустите сервисы:
   ```docker compose -f infra/docker-compose.yml up -d --build```

## Локальный запуск для разработки

```dotnet restore src/RedmineParser.slnx```
```dotnet run --project src/RedmineParser.WebApi```

## Конфигурация
Основные переменные окружения (infra/.env.example):
- `Redmine__Url`: базовый URL экземпляра Redmine
- `Redmine__ApiKey`: токен доступа пользователя Redmine
- `ConnectionStrings__DefaultConnection`: строка подключения к PostgreSQL

## CI/CD
- `test-app.yml`: запуск юнит-тестов при push или pull request в ветку main.
- `docker-support.yml`: сборка и валидация Docker-образа.

## Тестирование
```dotnet test src/RedmineParser.slnx --verbosity normal```

## База данных
Скрипт инициализации схемы находится в `infra/create_psql_table.sql`.
