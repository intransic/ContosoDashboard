---

description: "Task list for Document Upload and Management feature"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `specs/001-document-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are NOT requested in the feature specification - skip test tasks

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create AppData/uploads directory structure for document storage in ContosoDashboard/AppData/
- [X] T002 Add DocumentSettings configuration section to appsettings.json with MaxFileSize, TotalStorageQuota, AllowedFileExtensions, QuotaWarningThreshold

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create Document entity model in ContosoDashboard/Models/Document.cs with all fields from data-model.md
- [X] T004 Create DocumentShare entity model in ContosoDashboard/Models/DocumentShare.cs with all fields from data-model.md
- [X] T005 Add StorageUsed and StorageQuota fields to User entity in ContosoDashboard/Models/User.cs
- [X] T006 Update ApplicationDbContext in ContosoDashboard/Data/ApplicationDbContext.cs to add Document and DocumentShare DbSets with relationships
- [X] T007 [P] Create IFileStorageService interface in ContosoDashboard/Services/IFileStorageService.cs with UploadAsync, DeleteAsync, DownloadAsync, GetUrlAsync, GetStorageUsedAsync methods
- [X] T008 [P] Create LocalFileStorageService implementation in ContosoDashboard/Services/LocalFileStorageService.cs implementing IFileStorageService for local filesystem storage
- [X] T009 Create FileSignatureValidator in ContosoDashboard/Services/FileSignatureValidator.cs to validate file magic numbers and reject dangerous file types

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Upload and Capture Document Metadata (Priority: P1) 🎯 MVP

**Goal**: Employees can upload work documents and capture metadata (title, category, project, tags) so documents can be found, secured, and associated with projects

**Independent Test**: A user selects a supported file, provides required metadata, uploads it successfully, and then sees it listed in their personal document view

### Implementation for User Story 1

- [X] T010 [P] [US1] Create IDocumentService interface in ContosoDashboard/Services/IDocumentService.cs with UploadAsync, GetUserDocumentsAsync, GetProjectDocumentsAsync methods
- [X] T011 [P] [US1] Create DocumentService implementation in ContosoDashboard/Services/DocumentService.cs implementing IDocumentService with upload logic, file validation, storage quota checks
- [X] T012 [US1] Create IQuotaService interface in ContosoDashboard/Services/IQuotaService.cs with CanUploadAsync, GetUserStorageUsedAsync, GetUserStorageQuotaAsync, GetTotalStorageUsedAsync methods
- [X] T013 [US1] Create QuotaService implementation in ContosoDashboard/Services/QuotaService.cs implementing IQuotaService with quota validation logic
- [X] T014 [US1] Create DocumentController in ContosoDashboard/Controllers/DocumentController.cs with POST /api/documents/upload endpoint for file upload
- [X] T015 [US1] Add authorization attributes to DocumentController for authenticated users
- [X] T016 [US1] Create Documents.razor page in ContosoDashboard/Pages/Documents.razor to display user's uploaded documents with title, category, upload date, file size
- [X] T017 [US1] Add document upload form to Documents.razor with file input, title, category dropdown, description, project selection, tags fields

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Browse, Search, Preview, and Download Documents (Priority: P2)

**Goal**: Users can find and access documents they can view through browsing, filtering, search, preview, and download so they can reuse project assets

**Independent Test**: A user searches documents, filters by category or project, and successfully previews or downloads a document they have access to

### Implementation for User Story 2

- [ ] T018 [P] [US2] Add GetByIdAsync method to IDocumentService and DocumentService for retrieving single document
- [ ] T019 [P] [US2] Add SearchAsync method to IDocumentService and DocumentService for document search by title, description, tags, uploader, project
- [ ] T020 [US2] Add sorting (title, date, category, size) and filtering (category, project, date range) to Documents.razor page
- [ ] T021 [US2] Add GET /api/documents/search endpoint to DocumentController with query parameter
- [ ] T022 [US2] Add GET /api/documents/download/{id} endpoint to DocumentController for file download with authorization check
- [ ] T023 [US2] Add GET /api/documents/project/{projectId} endpoint to DocumentController for project documents view
- [ ] T024 [US2] Create ProjectDocuments.razor component in ContosoDashboard/Pages/ProjectDocuments.razor to display documents associated with a project

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Manage Document Access, Sharing, and Project Integration (Priority: P3)

**Goal**: Document owners can manage ownership, sharing, and project associations so documents stay secure and relevant to the right teams

**Independent Test**: A document owner edits metadata, shares it with another user or team, and that recipient receives a notification and can access the shared document

### Implementation for User Story 3

- [ ] T025 [P] [US3] Add UpdateAsync method to IDocumentService and DocumentService for editing document metadata
- [ ] T026 [P] [US3] Add ReplaceFileAsync method to IDocumentService and DocumentService for replacing document file
- [ ] T027 [US3] Add DeleteAsync method to IDocumentService and DocumentService for deleting documents with owner and Project Manager authorization
- [ ] T028 [US3] Add ShareAsync method to IDocumentService and DocumentService for sharing documents with users
- [ ] T029 [US3] Add GetSharedWithMeAsync method to IDocumentService and DocumentService for retrieving documents shared with current user
- [ ] T030 [US3] Add PUT /api/documents/{id} endpoint to DocumentController for updating document metadata
- [ ] T031 [US3] Add PUT /api/documents/{id}/replace endpoint to DocumentController for replacing document file
- [ ] T032 [US3] Add DELETE /api/documents/{id} endpoint to DocumentController for deleting documents
- [ ] T033 [US3] Add POST /api/documents/{id}/share endpoint to DocumentController for sharing documents
- [ ] T034 [US3] Add GET /api/documents/shared endpoint to DocumentController for shared documents view
- [ ] T035 [US3] Integrate with NotificationService to create in-app notifications when documents are shared
- [ ] T036 [US3] Add edit and delete buttons to Documents.razor with proper authorization checks (owner only for edit, owner and Project Manager for delete)
- [ ] T037 [US3] Add share dialog to Documents.razor to select users to share document with

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Integration with Existing Features

**Purpose**: Connect document management to dashboard, projects, and tasks

- [ ] T038 [P] Add Recent Documents widget to ContosoDashboard/Pages/Index.razor showing last 5 documents uploaded by user
- [ ] T039 [P] Add document count to dashboard summary cards in Index.razor
- [ ] T040 Add document attachment section to ContosoDashboard/Pages/Tasks.razor for viewing and attaching documents to tasks
- [ ] T041 Add document list display to ContosoDashboard/Pages/ProjectDetails.razor showing all documents for the project

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T042 [P] Add activity logging for document uploads, downloads, deletions, and shares in DocumentService
- [ ] T043 [P] Add total storage quota warning when system storage reaches threshold (configurable in appsettings.json)
- [ ] T044 Run quickstart.md validation to verify all implementation steps completed
- [ ] T045 Update README.md if feature changes alter the learning path or runtime behavior

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Integration (Phase 6)**: Depends on all user stories being complete
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - May integrate with US1 but should be independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - May integrate with US1/US2 but should be independently testable

### Within Each User Story

- Models before services
- Services before endpoints
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Once Foundational phase completes, all user stories can start in parallel (if team capacity allows)
- Models within a story marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members

---

## Parallel Example: User Story 1

```powershell
# After Phase 2 completes, US1 tasks can run in parallel:
# T010 (Create IDocumentService interface) - no dependencies
# T011 (Create DocumentService implementation) - depends on T010
# T012 (Create IQuotaService interface) - no dependencies
# T013 (Create QuotaService implementation) - depends on T012
# T014 (Create DocumentController) - depends on T010, T011
# T015 (Add authorization) - depends on T014
# T016 (Create Documents.razor) - depends on T010, T011
# T017 (Add upload form) - depends on T016
```

---

## Summary

| Phase | Task Count | Description |
|-------|------------|-------------|
| Phase 1: Setup | 2 | Project initialization |
| Phase 2: Foundational | 7 | Core infrastructure (BLOCKS user stories) |
| Phase 3: US1 (P1) | 8 | Upload and metadata capture |
| Phase 4: US2 (P2) | 7 | Browse, search, preview, download |
| Phase 5: US3 (P3) | 13 | Access control, sharing, management |
| Phase 6: Integration | 4 | Dashboard, project, task integration |
| Phase 7: Polish | 4 | Cross-cutting concerns |
| **Total** | **45** | |

### Independent Test Criteria

- **US1 (P1)**: User can upload a document with title and category, see it in My Documents list
- **US2 (P2)**: User can search for a document, filter by category, preview PDF/image, download file
- **US3 (P3)**: User can edit document metadata, share with another user, receive notification, delete own document

### MVP Scope

- **MVP**: User Story 1 (P1) - Upload and metadata capture
- Complete upload flow with validation, quota checks, file storage
- My Documents view showing uploaded documents
- This delivers the core document management capability