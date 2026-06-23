# Reinforcement Learning Ball Agent: Navegación Autónoma en 3D

<video src="demo-agente.mp4" width="600" controls="controls"></video>

## ¿De qué trata este proyecto?
Este repositorio contiene un agente de Inteligencia Artificial que aprendió desde cero a navegar por un circuito 3D continuo. Construido en **Unity** con **ML-Agents**, la IA controla una esfera que debe llegar a la meta superando curvas, inercias e imperfecciones en la pista, basándose únicamente en un sistema de prueba y error estructurado (Aprendizaje por Refuerzo).

## El Reto Técnico
Enseñar a un sistema a moverse en un entorno con físicas reales presenta obstáculos específicos de ingeniería:
* **Inercia y Control Continuo:** La IA no se mueve sobre rieles preprogramados. Debe calcular la fuerza, acelerar, frenar y compensar la gravedad en tiempo real.
* **Geometría Defectuosa:** La pista modular contiene uniones superpuestas y escalones. El agente tuvo que aprender a superar estas imperfecciones físicas sin perder tracción.
* **El problema del "Agente Perezoso":** En etapas iniciales, la IA calcula que quedarse quieta es más seguro para no perder puntos cayendo al vacío, paralizando el entrenamiento.

## La Solución Implementada
Para lograr la autonomía del sistema, diseñé e integré las siguientes soluciones en la arquitectura del código (C# y Python):
* **Sistema de Recompensas Densas:** En lugar de premiar a la IA solo al llegar a la meta, el script otorga fracciones de punto por cada milímetro que avanza hacia el siguiente *checkpoint* y penaliza cada milisegundo de retraso, forzando un comportamiento agresivo y rápido.
* **Mecanismo Anti-Estancamiento:** Desarrollé un temporizador de inactividad que detecta si la bola pierde velocidad tangencial durante 800 pasos lógicos (atascos en curvas). Si ocurre, la instancia se purga y reinicia automáticamente, evitando el desperdicio de ciclos de procesamiento.
* **Sobremuestreo del Motor Físico:** Se alteró el *Fixed Timestep* de Unity a 100Hz para duplicar la precisión de las colisiones, evitando el efecto de tunelización a altas velocidades.

## Contenido del Repositorio (Optimización de Clonación)
Para garantizar una distribución ágil y cumplir con los estándares de GitHub, este repositorio opera bajo una estructura optimizada:
* **Elementos Incluidos:** El código fuente C# (la lógica del agente en `BallCircuitMaster.cs`), el archivo YAML de hiperparámetros de PyTorch, las configuraciones base de la escena de Unity y el cerebro neuronal final exportado (`.onnx`), el cual está listo para ejecutarse en modo inferencia.
* **Elementos Excluidos (Por diseño):** Mediante reglas estrictas en el `.gitignore`, se excluyeron los archivos temporales de compilación de Unity (`Library/`, `Temp/`, etc.) y los puntos de control de entrenamiento intermedios (`.pt`). Estos archivos alcanzan dimensiones masivas (gigabytes) y son innecesarios para demostrar la ejecución autónoma del algoritmo final.

## Resultados 
El modelo se entrenó localmente optimizando el uso de recursos de hardware. Tras iterar durante más de **41 millones de pasos**, la red neuronal convergió con éxito. El archivo exportado (`.onnx`) permite al agente recorrer el circuito de manera fluida y 100% autónoma.

**Tecnologías:** Unity 3D, C#, Python, PyTorch, Proximal Policy Optimization (PPO).
