CREATE DATABASE chatbot WITH
  ENCODING='UTF8'
  LC_COLLATE='Portuguese_Brazil.1252'
  LC_CTYPE='Portuguese_Brazil.1252'
  TEMPLATE template0
;
\c chatbot;
CREATE TABLE IF NOT EXISTS usuarios(
  rowid SERIAL PRIMARY KEY,
  identifier BIGINT NOT NULL,
  create_at TIMESTAMP NOT NULL,
  update_at TIMESTAMP NOT NULL,
  privilege INT DEFAULT 0,
  inserted_by BIGINT DEFAULT 0,
  phone_number BIGINT DEFAULT 0,
  username VARCHAR(64) DEFAULT ''
);
CREATE TABLE IF NOT EXISTS solicitacoes(
  rowid SERIAL PRIMARY KEY,
  identifier BIGINT NOT NULL,
  application VARCHAR(16) NOT NULL,
  information BIGINT DEFAULT 0,
  request_type INT DEFAULT 0,
  received_at TIMESTAMP NOT NULL,
  response_at TIMESTAMP DEFAULT NULL,
  status INT DEFAULT 0,
  instance INT DEFAULT 0
);
CREATE TABLE IF NOT EXISTS faturas(
  rowid SERIAL PRIMARY KEY,
  filename VARCHAR(64) NOT NULL,
  instalation BIGINT NOT NULL,
  timestamp TIMESTAMP NOT NULL,
  status INT DEFAULT 0
);
