<?php
require_once "db.php";

$stmt = $pdo->query("SELECT id, name, created_at FROM players ORDER BY id DESC LIMIT 20");
$rows = $stmt->fetchAll(PDO::FETCH_ASSOC);

echo json_encode(["ok" => true, "players" => $rows]);
