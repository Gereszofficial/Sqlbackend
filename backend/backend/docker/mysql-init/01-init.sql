-- App DB + Sandbox DB + users
CREATE DATABASE IF NOT EXISTS sqltrainer_app CHARACTER SET utf8mb4 COLLATE utf8mb4_hungarian_ci;
CREATE DATABASE IF NOT EXISTS sqltrainer_sandbox CHARACTER SET utf8mb4 COLLATE utf8mb4_hungarian_ci;

CREATE USER IF NOT EXISTS 'sqltrainer_app'@'%' IDENTIFIED BY 'Strong_App_Pass!';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, DROP ON sqltrainer_app.* TO 'sqltrainer_app'@'%';

CREATE USER IF NOT EXISTS 'sqltrainer_runner'@'%' IDENTIFIED BY 'Strong_Runner_Pass!';
GRANT CREATE, DROP, ALTER, INDEX, SELECT, INSERT, UPDATE, DELETE, CREATE TEMPORARY TABLES ON sqltrainer_sandbox.* TO 'sqltrainer_runner'@'%';

FLUSH PRIVILEGES;
