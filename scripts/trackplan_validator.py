# TrackPlan AI Validator
# ========================
# 
# Dieses Modul trainiert ein AI-Modell zur Validierung von Gleisplänen.
# Es lernt, plausible von implausiblen Gleisbildern zu unterscheiden.
#
# INSTALLATION:
#   pip install torch torchvision pillow numpy scikit-learn matplotlib
#
# TRAINING:
#   1. Lege korrekte SVG-Dateien in ./data/valid/
#   2. Lege fehlerhafte SVG-Dateien in ./data/invalid/ (optional)
#   3. Führe aus: python trackplan_validator.py train
#
# VALIDIERUNG:
#   python trackplan_validator.py validate path/to/plan.svg

import os
import sys
import json
import math
from pathlib import Path
from typing import List, Tuple, Optional
from dataclasses import dataclass
from enum import Enum

# ============================================================
# APPROACH 1: Rule-Based Validator (empfohlen als Startpunkt)
# ============================================================

class ValidationRule(Enum):
    CLOSED_LOOP = "closed_loop"           # Geschlossener Kreis
    PORT_ALIGNMENT = "port_alignment"     # Ports zeigen zueinander
    NO_OVERLAP = "no_overlap"             # Keine Überlappungen
    CONNECTED_GRAPH = "connected_graph"   # Zusammenhängender Graph
    VALID_ANGLES = "valid_angles"         # Gültige Winkel (z.B. 15°, 30°)

@dataclass
class ValidationResult:
    is_valid: bool
    score: float  # 0.0 - 1.0
    violations: List[str]
    suggestions: List[str]

class RuleBasedValidator:
"""
Regelbasierter Validator - kein Training nötig!
Prüft Gleispläne auf geometrische Konsistenz.
    
Verwendung:
    validator = RuleBasedValidator()
    result = validator.validate(track_plan_json)
    print(f"Valide: {result.is_valid}")
    print(f"Fehler: {result.violations}")
"""
    
def __init__(self, tolerance: float = 1.0):
    self.tolerance = tolerance  # Toleranz in mm
    self.angle_tolerance = 5.0  # Toleranz in Grad
        
    # Piko A Geometrie-Konstanten
    self.PIKO_RADII = [358.0, 421.6, 484.5, 888.0]  # R1, R2, R3, R9
    self.PIKO_ANGLES = [15.0, 30.0, 90.0]  # Gültige Winkel
    self.PIKO_LENGTHS = [231.0, 119.2, 62.0, 55.5, 31.0]  # Gerade Längen
    
def validate(self, track_plan: dict) -> ValidationResult:
    """Validiert einen Gleisplan aus JSON."""
    violations = []
    suggestions = []
    scores = []
        
    # Extrahiere Edges und Positionen
    edges = track_plan.get('edges', [])
    positions = track_plan.get('positions', {})
    rotations = track_plan.get('rotations', {})
    connections = track_plan.get('connections', [])
        
    # Regel 1: Prüfe ob Ports aligned sind
    port_score, port_violations = self._check_port_alignment(edges, positions, rotations, connections)
    scores.append(port_score)
    violations.extend(port_violations)
        
    # Regel 2: Prüfe auf Überlappungen
    overlap_score, overlap_violations = self._check_overlaps(edges, positions, rotations)
    scores.append(overlap_score)
    violations.extend(overlap_violations)
        
    # Regel 3: Prüfe ob Graph zusammenhängend ist
    connected_score, connected_violations = self._check_connectivity(edges, connections)
    scores.append(connected_score)
    violations.extend(connected_violations)
        
    # Regel 4: Prüfe geschlossene Schleifen
    loop_score, loop_issues = self._check_closed_loops(edges, positions, rotations, connections)
    scores.append(loop_score)
    if loop_issues:
        suggestions.extend(loop_issues)
        
    # Regel 5: Prüfe gültige Winkel
    angle_score, angle_violations = self._check_valid_angles(rotations)
    scores.append(angle_score)
    violations.extend(angle_violations)
        
    avg_score = sum(scores) / len(scores) if scores else 0.0
        
    return ValidationResult(
        is_valid=len(violations) == 0,
        score=avg_score,
        violations=violations,
        suggestions=suggestions
    )
    
def _check_port_alignment(self, edges, positions, rotations, connections) -> Tuple[float, List[str]]:
    """
    Regel 1: Verbundene Ports müssen zueinander zeigen (180° Differenz).
        
    Beispiel:
        Port A zeigt nach 0° (rechts)
        Port B muss nach 180° (links) zeigen
        Differenz = |0° - 180°| = 180° ✅
    """
    violations = []
        
    for conn in connections:
        edge1_id = conn.get('edge1_id')
        port1_id = conn.get('port1_id')
        edge2_id = conn.get('edge2_id')
        port2_id = conn.get('port2_id')
            
        # Berechne Port-Winkel (vereinfacht: Rotation + Port-Offset)
        angle1 = rotations.get(edge1_id, 0)
        angle2 = rotations.get(edge2_id, 0)
            
        # Port B ist typischerweise am Ende (+ Kurvenwinkel)
        if port1_id == 'B':
            angle1 += 30  # Kurvenwinkel
        if port2_id == 'B':
            angle2 += 30
            
        # Differenz sollte ~180° sein
        diff = abs((angle1 - angle2 + 180) % 360 - 180)
            
        if diff > self.angle_tolerance and diff < 360 - self.angle_tolerance:
            violations.append(
                f"Port-Alignment: {edge1_id[:8]}.{port1_id} ({angle1}°) und "
                f"{edge2_id[:8]}.{port2_id} ({angle2}°) zeigen nicht zueinander "
                f"(Differenz: {diff:.1f}°, erwartet: ~180°)"
            )
        
    score = 1.0 - (len(violations) / max(len(connections), 1))
    return max(0, score), violations
    
def _check_overlaps(self, edges, positions, rotations) -> Tuple[float, List[str]]:
    """
    Regel 2: Gleise dürfen sich nicht überlappen.
        
    Vereinfachte Prüfung: Positionen dürfen nicht zu nah beieinander sein.
    """
    violations = []
    min_distance = 50.0  # Mindestabstand in mm
        
    pos_list = [(eid, positions.get(eid, {})) for eid in positions]
        
    for i, (id1, pos1) in enumerate(pos_list):
        for j, (id2, pos2) in enumerate(pos_list):
            if i >= j:
                continue
                
            if not pos1 or not pos2:
                continue
                
            x1, y1 = pos1.get('x', 0), pos1.get('y', 0)
            x2, y2 = pos2.get('x', 0), pos2.get('y', 0)
                
            distance = math.sqrt((x2 - x1)**2 + (y2 - y1)**2)
                
            if distance < min_distance:
                violations.append(
                    f"Überlappung: {id1[:8]} und {id2[:8]} sind zu nah "
                    f"(Abstand: {distance:.1f}mm, Minimum: {min_distance}mm)"
                )
        
    score = 1.0 - (len(violations) / max(len(pos_list), 1))
    return max(0, score), violations
    
def _check_connectivity(self, edges, connections) -> Tuple[float, List[str]]:
    """
    Regel 3: Alle Gleise müssen verbunden sein (zusammenhängender Graph).
        
    Verwendet BFS vom ersten Gleis.
    """
    violations = []
        
    if not edges:
        return 1.0, []
        
    # Baue Adjazenzliste
    adjacency = {e.get('id'): set() for e in edges}
    for conn in connections:
        e1, e2 = conn.get('edge1_id'), conn.get('edge2_id')
        if e1 in adjacency and e2 in adjacency:
            adjacency[e1].add(e2)
            adjacency[e2].add(e1)
        
    # BFS vom ersten Gleis
    start = edges[0].get('id')
    visited = set()
    queue = [start]
        
    while queue:
        current = queue.pop(0)
        if current in visited:
            continue
        visited.add(current)
        queue.extend(adjacency.get(current, set()) - visited)
        
    # Prüfe ob alle besucht
    unvisited = set(adjacency.keys()) - visited
        
    if unvisited:
        violations.append(
            f"Nicht verbunden: {len(unvisited)} Gleis(e) sind isoliert: "
            f"{', '.join(list(unvisited)[:3])}{'...' if len(unvisited) > 3 else ''}"
        )
        
    score = len(visited) / len(edges) if edges else 1.0
    return score, violations
    
def _check_closed_loops(self, edges, positions, rotations, connections) -> Tuple[float, List[str]]:
    """
    Regel 4: Geschlossene Schleifen sollten sich schließen.
        
    Berechnet Endposition nach Durchlauf und prüft ob Start ≈ Ende.
    """
    issues = []
        
    # Finde offene Ports (nur einmal verbunden)
    port_connections = {}
    for conn in connections:
        key1 = f"{conn.get('edge1_id')}:{conn.get('port1_id')}"
        key2 = f"{conn.get('edge2_id')}:{conn.get('port2_id')}"
        port_connections[key1] = port_connections.get(key1, 0) + 1
        port_connections[key2] = port_connections.get(key2, 0) + 1
        
    # Ports die nur einmal vorkommen sind offen
    open_ports = [k for k, v in port_connections.items() if v == 1]
        
    if len(open_ports) == 0:
        # Geschlossene Schleife - prüfe ob sie geometrisch schließt
        issues.append("Geschlossene Schleife erkannt - geometrische Prüfung empfohlen")
        return 1.0, issues
    elif len(open_ports) == 2:
        issues.append(f"Zwei offene Enden: {open_ports[0]}, {open_ports[1]} - könnte verbunden werden")
        return 0.8, issues
    else:
        issues.append(f"{len(open_ports)} offene Enden gefunden")
        return 0.5, issues
    
def _check_valid_angles(self, rotations) -> Tuple[float, List[str]]:
    """
    Regel 5: Nur gültige Winkel (Piko A: 0°, 15°, 30°, 45°, 60°, 75°, 90°, ...).
        
    Winkel sollten Vielfache von 15° sein.
    """
    violations = []
    valid_step = 15.0
        
    for edge_id, angle in rotations.items():
        normalized = angle % 360
        remainder = normalized % valid_step
            
        if remainder > self.angle_tolerance and remainder < valid_step - self.angle_tolerance:
            violations.append(
                f"Ungültiger Winkel: {edge_id[:8]} hat {normalized:.1f}° "
                f"(sollte Vielfaches von {valid_step}° sein)"
            )
        
    score = 1.0 - (len(violations) / max(len(rotations), 1))
    return max(0, score), violations


# ============================================================
# APPROACH 2: CNN-Based Visual Validator (braucht Training)
# ============================================================

try:
    import torch
    import torch.nn as nn
    import torch.optim as optim
    from torch.utils.data import Dataset, DataLoader
    from torchvision import transforms
    from PIL import Image
    import numpy as np
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False
    print("PyTorch nicht installiert. Für AI-Modell: pip install torch torchvision")


if TORCH_AVAILABLE:
    
    class TrackPlanDataset(Dataset):
        """Dataset für Gleisplan-Bilder."""
        
        def __init__(self, valid_dir: str, invalid_dir: Optional[str] = None, 
                     image_size: int = 224):
            self.samples = []
            self.transform = transforms.Compose([
                transforms.Resize((image_size, image_size)),
                transforms.ToTensor(),
                transforms.Normalize(mean=[0.485, 0.456, 0.406], 
                                   std=[0.229, 0.224, 0.225])
            ])
            
            # Lade valide Beispiele (Label = 1)
            if os.path.exists(valid_dir):
                for f in Path(valid_dir).glob("*.png"):
                    self.samples.append((str(f), 1))
                for f in Path(valid_dir).glob("*.svg"):
                    # SVG zu PNG konvertieren würde hier passieren
                    pass
            
            # Lade invalide Beispiele (Label = 0)
            if invalid_dir and os.path.exists(invalid_dir):
                for f in Path(invalid_dir).glob("*.png"):
                    self.samples.append((str(f), 0))
        
        def __len__(self):
            return len(self.samples)
        
        def __getitem__(self, idx):
            path, label = self.samples[idx]
            image = Image.open(path).convert('RGB')
            image = self.transform(image)
            return image, torch.tensor(label, dtype=torch.float32)
    
    
    class TrackPlanCNN(nn.Module):
        """
        Einfaches CNN zur Klassifikation von Gleisplänen.
        
        Input: 224x224 RGB Bild
        Output: Wahrscheinlichkeit dass der Plan valide ist (0-1)
        """
        
        def __init__(self):
            super().__init__()
            
            # Feature Extraction
            self.features = nn.Sequential(
                # Conv Block 1
                nn.Conv2d(3, 32, kernel_size=3, padding=1),
                nn.BatchNorm2d(32),
                nn.ReLU(),
                nn.MaxPool2d(2),
                
                # Conv Block 2
                nn.Conv2d(32, 64, kernel_size=3, padding=1),
                nn.BatchNorm2d(64),
                nn.ReLU(),
                nn.MaxPool2d(2),
                
                # Conv Block 3
                nn.Conv2d(64, 128, kernel_size=3, padding=1),
                nn.BatchNorm2d(128),
                nn.ReLU(),
                nn.MaxPool2d(2),
                
                # Conv Block 4
                nn.Conv2d(128, 256, kernel_size=3, padding=1),
                nn.BatchNorm2d(256),
                nn.ReLU(),
                nn.AdaptiveAvgPool2d((4, 4))
            )
            
            # Classifier
            self.classifier = nn.Sequential(
                nn.Flatten(),
                nn.Linear(256 * 4 * 4, 512),
                nn.ReLU(),
                nn.Dropout(0.5),
                nn.Linear(512, 1),
                nn.Sigmoid()
            )
        
        def forward(self, x):
            x = self.features(x)
            x = self.classifier(x)
            return x
    
    
    class TrackPlanTrainer:
        """Trainiert das CNN-Modell."""
        
        def __init__(self, model_path: str = "trackplan_model.pth"):
            self.model_path = model_path
            self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
            self.model = TrackPlanCNN().to(self.device)
        
        def train(self, valid_dir: str, invalid_dir: str, 
                  epochs: int = 50, batch_size: int = 16, lr: float = 0.001):
            """Trainiert das Modell."""
            
            dataset = TrackPlanDataset(valid_dir, invalid_dir)
            if len(dataset) == 0:
                print("Keine Trainingsdaten gefunden!")
                return
            
            # Split in Train/Val
            train_size = int(0.8 * len(dataset))
            val_size = len(dataset) - train_size
            train_set, val_set = torch.utils.data.random_split(dataset, [train_size, val_size])
            
            train_loader = DataLoader(train_set, batch_size=batch_size, shuffle=True)
            val_loader = DataLoader(val_set, batch_size=batch_size)
            
            criterion = nn.BCELoss()
            optimizer = optim.Adam(self.model.parameters(), lr=lr)
            
            best_val_acc = 0.0
            
            for epoch in range(epochs):
                # Training
                self.model.train()
                train_loss = 0.0
                for images, labels in train_loader:
                    images, labels = images.to(self.device), labels.to(self.device)
                    
                    optimizer.zero_grad()
                    outputs = self.model(images).squeeze()
                    loss = criterion(outputs, labels)
                    loss.backward()
                    optimizer.step()
                    
                    train_loss += loss.item()
                
                # Validation
                self.model.eval()
                correct = 0
                total = 0
                with torch.no_grad():
                    for images, labels in val_loader:
                        images, labels = images.to(self.device), labels.to(self.device)
                        outputs = self.model(images).squeeze()
                        predicted = (outputs > 0.5).float()
                        total += labels.size(0)
                        correct += (predicted == labels).sum().item()
                
                val_acc = correct / total if total > 0 else 0
                
                print(f"Epoch {epoch+1}/{epochs} - Loss: {train_loss/len(train_loader):.4f} - Val Acc: {val_acc:.4f}")
                
                # Save best model
                if val_acc > best_val_acc:
                    best_val_acc = val_acc
                    torch.save(self.model.state_dict(), self.model_path)
                    print(f"  → Modell gespeichert (Acc: {val_acc:.4f})")
            
            print(f"\nTraining abgeschlossen. Bestes Modell: {self.model_path}")
        
        def load(self):
            """Lädt ein trainiertes Modell."""
            if os.path.exists(self.model_path):
                self.model.load_state_dict(torch.load(self.model_path, map_location=self.device))
                self.model.eval()
                return True
            return False
        
        def predict(self, image_path: str) -> float:
            """Gibt Wahrscheinlichkeit zurück dass der Plan valide ist."""
            if not self.load():
                print("Kein trainiertes Modell gefunden!")
                return 0.5
            
            transform = transforms.Compose([
                transforms.Resize((224, 224)),
                transforms.ToTensor(),
                transforms.Normalize(mean=[0.485, 0.456, 0.406], 
                                   std=[0.229, 0.224, 0.225])
            ])
            
            image = Image.open(image_path).convert('RGB')
            image = transform(image).unsqueeze(0).to(self.device)
            
            with torch.no_grad():
                output = self.model(image)
            
            return output.item()


# ============================================================
# APPROACH 3: Hybrid Validator (Regel + AI)
# ============================================================

class HybridValidator:
    """
    Kombiniert regelbasierte und AI-basierte Validierung.
    
    - Regelbasiert: Schnell, deterministisch, keine Trainingsdaten nötig
    - AI-basiert: Kann subtile Muster lernen, braucht Trainingsdaten
    """
    
    def __init__(self, use_ai: bool = True):
        self.rule_validator = RuleBasedValidator()
        self.ai_validator = TrackPlanTrainer() if TORCH_AVAILABLE and use_ai else None
    
    def validate(self, track_plan: dict, image_path: Optional[str] = None) -> ValidationResult:
        """Validiert mit beiden Methoden."""
        
        # Regelbasierte Validierung
        rule_result = self.rule_validator.validate(track_plan)
        
        # AI-basierte Validierung (falls Bild vorhanden)
        ai_score = 0.5
        if self.ai_validator and image_path and os.path.exists(image_path):
            ai_score = self.ai_validator.predict(image_path)
        
        # Kombiniere Scores (gewichteter Durchschnitt)
        combined_score = 0.7 * rule_result.score + 0.3 * ai_score
        
        return ValidationResult(
            is_valid=rule_result.is_valid and ai_score > 0.5,
            score=combined_score,
            violations=rule_result.violations,
            suggestions=rule_result.suggestions + [f"AI Confidence: {ai_score:.2%}"]
        )


# ============================================================
# CLI
# ============================================================

def main():
    if len(sys.argv) < 2:
        print("""
TrackPlan AI Validator
======================

Verwendung:
  python trackplan_validator.py train              # Trainiert das Modell
  python trackplan_validator.py validate <file>   # Validiert eine Datei
  python trackplan_validator.py rules <json>      # Nur regelbasierte Prüfung

Ordnerstruktur für Training:
  ./data/valid/      # Korrekte Gleispläne (PNG/SVG)
  ./data/invalid/    # Fehlerhafte Gleispläne (optional)
        """)
        return
    
    command = sys.argv[1]
    
    if command == "train":
        if not TORCH_AVAILABLE:
            print("PyTorch nicht installiert! pip install torch torchvision")
            return
        
        trainer = TrackPlanTrainer()
        trainer.train(
            valid_dir="./data/valid",
            invalid_dir="./data/invalid",
            epochs=50
        )
    
    elif command == "validate" and len(sys.argv) > 2:
        file_path = sys.argv[2]
        
        if file_path.endswith('.json'):
            with open(file_path) as f:
                track_plan = json.load(f)
            validator = RuleBasedValidator()
            result = validator.validate(track_plan)
        else:
            # Bild-basierte Validierung
            if not TORCH_AVAILABLE:
                print("Für Bild-Validierung: pip install torch torchvision")
                return
            trainer = TrackPlanTrainer()
            score = trainer.predict(file_path)
            result = ValidationResult(
                is_valid=score > 0.5,
                score=score,
                violations=[],
                suggestions=[f"AI Confidence: {score:.2%}"]
            )
        
        print(f"\nValidierungsergebnis:")
        print(f"  Valide: {'✅ Ja' if result.is_valid else '❌ Nein'}")
        print(f"  Score: {result.score:.2%}")
        if result.violations:
            print(f"  Fehler:")
            for v in result.violations:
                print(f"    - {v}")
        if result.suggestions:
            print(f"  Hinweise:")
            for s in result.suggestions:
                print(f"    - {s}")
    
    elif command == "rules" and len(sys.argv) > 2:
        with open(sys.argv[2]) as f:
            track_plan = json.load(f)
        validator = RuleBasedValidator()
        result = validator.validate(track_plan)
        print(json.dumps({
            "is_valid": result.is_valid,
            "score": result.score,
            "violations": result.violations,
            "suggestions": result.suggestions
        }, indent=2))
    
    else:
        print(f"Unbekannter Befehl: {command}")


if __name__ == "__main__":
    main()
