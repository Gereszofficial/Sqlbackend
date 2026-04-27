# SqlTrainer Frontend (Vue 3 + Vite + Pinia + Tailwind)

## Telepítés
1) `npm install`
2) másold: `.env.example` -> `.env`
3) `npm run dev`

## API base URL
`.env`:
`VITE_API_BASE=https://localhost:64628`

## HTTPS tipp
Ha self-signed cert van: nyisd meg az API Swaggerét és fogadd el a böngésző figyelmeztetését:
`https://localhost:64628/swagger`

## Oldalak
- /login, /register
- Student: /student/tasks, /student/tasks/:id (run/submit)
- Admin: /admin/tasks, /admin/tasks/new, /admin/tasks/:id, /admin/submissions
