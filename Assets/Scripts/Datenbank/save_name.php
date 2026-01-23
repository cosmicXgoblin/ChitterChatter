<?php
require_once "db.php";

$raw = file_get_contents("php://input");
$data = json_decode($raw, true);

if (!is_array($data) || !isset($data["name"])) {
  http_response_code(400);
  echo json_encode(["ok" => false, "error" => "Missing name"]);
  exit;
}

$name = trim($data["name"]);

if ($name === "" || mb_strlen($name) > 50) {
  http_response_code(400);
  echo json_encode(["ok" => false, "error" => "Invalid name"]);
  exit;
}

$stmt = $pdo->prepare("INSERT INTO players (name) VALUES (?)");
$stmt->execute([$name]);

echo json_encode(["ok" => true, "id" => $pdo->lastInsertId()]);
