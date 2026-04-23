<!--
Version change: none → 1.0.0
Modified principles: added all five principles for training, security, specification, simplicity, and observability
Added sections: Training Constraints, Development Workflow
Removed sections: none
Templates requiring updates:
- .specify/templates/plan-template.md ✅ reviewed, no constitution-driven update required
- .specify/templates/spec-template.md ✅ reviewed, no constitution-driven update required
- .specify/templates/tasks-template.md ✅ reviewed, no constitution-driven update required
- .specify/templates/constitution-template.md ✅ no update required for generated content
Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training Integrity
All changes MUST preserve the repository’s purpose as an offline training application. The project MUST remain safe for learners by avoiding production-only assumptions, keeping mock authentication and LocalDB isolation intact, and clearly documenting any limits of the training implementation.

### II. Security by Design
Every feature MUST enforce authorization and protect against common web risks such as IDOR and unauthorized data access. Security behavior in this repository is explicitly training-focused: mock implementations are allowed for learning, but production-grade requirements MUST be documented as out of scope.

### III. Specification-First Development
All work MUST begin with a Spec Kit feature specification that captures user scenarios, acceptance criteria, and measurable outcomes. Design decisions and implementation plans MUST be derived from the specification before code is written.

### IV. Simplicity and Clarity
Architecture, data models, and UI flows MUST remain as simple as possible for learners while still demonstrating best practices. The repository MUST avoid unnecessary abstractions, external dependencies, and complexity that would obscure the training value.

### V. Observability and Maintainability
Every feature MUST include the minimum required observability, documentation, and separation of concerns needed for debugging and future learning. Code changes MUST be accompanied by updated guidance, and runtime behavior MUST be explainable to trainees.

## Training Constraints

This repository is explicitly a training implementation and is NOT production-ready. The following constraints are mandatory:
- Authentication is mock-only and designed for educational scenarios, not for real-world identity providers.
- Data storage MUST remain local (SQL Server LocalDB or equivalent training-safe storage); external cloud dependencies are prohibited in core training scenarios.
- Production migration pathways may be described, but production system assumptions MUST be separated from the training codebase.
- Any addition that depends on external services or enterprise infrastructure MUST include a clear training-only disclaimer.

## Development Workflow

Development MUST follow a feature-driven workflow with spec, plan, and task artifacts:
- Create and maintain feature specs, plans, and tasks in the Spec Kit folder structure.
- Use feature branches and PR review for all changes.
- Link each PR to the Constitution principles that are most relevant to the change.
- Update README or training guidance whenever a change alters the learning path, security assumptions, or runtime behavior.

## Governance

This Constitution supersedes informal practices in the repository. Amendments MUST be documented in this file and reviewed before merging.
- Amendments that redefine principles or add new governance obligations MUST use semantic versioning with a major bump.
- Amendments that add new sections, expand workflow expectations, or strengthen procedure MUST use a minor bump.
- Clarifications, wording improvements, and non-substantive refinements MUST use a patch bump.
- Every PR affecting core behavior or training guidance MUST reference the applicable Constitution principles and explain how compliance is maintained.
- Reviewers MUST verify that changes preserve the repository’s training scope and do not introduce hidden production assumptions.

**Version**: 1.0.0 | **Ratified**: 2026-04-23 | **Last Amended**: 2026-04-23
