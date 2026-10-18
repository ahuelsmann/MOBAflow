# Input, requirement and acceptance traceability

Source: [#337](https://github.com/ahuelsmann/MOBAflow/issues/337). Sources P1-P7 and their limits are in [decisions](decisions.md). Every implementation task below is OPEN; qualification Q01-Q25 is NOT RUN.

## Claude consultation: all ten points

| Input | Requirement / design location | Tasks | Later acceptance oracle |
| --- | --- | --- | --- |
| C1 current role authority and later governance inventory | spec actors; plan governance inventory, no governance writes | T001/T002/T010/T042 | Q01; exact planning diff excludes governance |
| C2 actual PC dependency/dispatch authority and one cutover | FR-016; pc-dependencies; plan cutover/rollback | T004/T032-T035 | Q20/Q21, actual authority not heartbeat-name assumption |
| C3 scanner available/authenticated, strict fail closed | FR-008/009; lifecycle scan boundary | T003/T007/T020 | Q07/Q08 on every proposed environment |
| C4 every external mutation precondition/revocation/atomicity/fencing/reconcile | FR-005/007/009/024; M01-M18 | T007/T018/T020-T024 | Q03/Q04/Q06/Q08/Q10/Q11, unsupported excluded |
| C5 zero taskstoissues now, later Senior decision | FR-023; plan/task scope | T001/T032/T042 | SC-010, zero new issues |
| C6 failed build/missing-empty SARIF and unchanged baseline | FR-014; research reported/unverified | T025 | Q13; no assumed confirmed defect/fix |
| C7 honest gate exits including staged line endings | FR-011/014; lifecycle gate inventory | T008/T014/T026/T040/T041 | Q12/Q14, zero-tests/finding-exit-zero fail |
| C8 branch owner/allocation/old-new SHA, safe preconditions/no force/foreign incident | FR-013; M01-M04/M16 | T018/T020/T022 | Q10/Q11 |
| C9 original recorded human decision + allocation, not agent relay | FR-001/017/024; authority format/data model | T002/T009-T012/T028 | Q01/Q17 |
| C10 reproducible SDK/workloads/images/short path/Windows-Android gates and later canary | FR-015; research/qualification | T004/T027/T040 | Q15, both platform targets; NOT RUN now |

Fresh consultation C1-C5 and original session experience C6-C10 remain distinct. P1 incorporates seven leadership inputs and Gemini, so individual unpublished authorship is not invented.

## Complete functional coverage

| Requirement | Tasks | Qualification |
| --- | --- | --- |
| FR-001 | T002/T009-T012 | Q01 |
| FR-002 | T006/T012/T033/T034 | Q02/Q21 |
| FR-003 | T011/T012/T020/T021 | Q02/Q03 |
| FR-004 | T008/T013/T019/T029 | Q12/Q19 |
| FR-005 | T007/T018/T020-T022 | Q03/Q04/Q06 |
| FR-006 | T021/T023/T024 | Q09/Q11 |
| FR-007 | T006/T022/T024/T028/T029 | Q05/Q06/Q19 |
| FR-008 | T003/T007/T020 | Q07 |
| FR-009 | T003/T007/T020/T021 | Q04/Q08 |
| FR-010 | T015/T016/T018/T019/T037/T042 | Q11/Q12/Q24 |
| FR-011 | T008/T014/T016/T019/T026/T040/T041 | Q12/Q14 |
| FR-012 | T011/T017/T019/T032 | Q20/Q22 |
| FR-013 | T018/T020/T022 | Q10/Q11 |
| FR-014 | T025/T026 | Q13/Q14 |
| FR-015 | T004/T027/T040 | Q15 |
| FR-016 | T004/T032-T035 | Q20/Q21 |
| FR-017 | T002/T028/T029 | Q01/Q17 |
| FR-018 | T001/T006/T030/T031/T037 | Q16/Q18 |
| FR-019 | T013/T028/T029/T037 | Q17/Q19 |
| FR-020 | T018/T019/T039 | Q23 |
| FR-021 | T036/T037/T042 | Q24 |
| FR-022 | T017/T019/T038/T039/T042 | Q25 |
| FR-023 | T001/T004/T032/T042 | SC-010 planning-only checks |
| FR-024 | T002/T007/T010/T012/T020/T022 | Q01/Q03/Q08/Q22 |

## Success criteria and stories

| Outcome | Story / tasks | Method / retained limit |
| --- | --- | --- |
| SC-001 | US1, T002/T009-T012 | Q01; original human authority |
| SC-002 | US1/US3/US5, T006/T012/T020/T021/T033/T034 | Q02-Q04/Q21, actual exclusion |
| SC-003 | US5, T032-T037 | Q20/Q22; one actual approved PC-off issue, manual gates preserved |
| SC-004 | US3, T020-T024 | Q03-Q06/Q09-Q11, unknown remains blocked |
| SC-005 | US2, T014-T019/T041 | Q11/Q12, exact comparison/model/end |
| SC-006 | US3, T003/T020/T025-T027 | Q07/Q13-Q15, no cloud qualification yet |
| SC-007 | US4, T028-T031 | Q16-Q19, budget/human decisions open |
| SC-008 | US3/US5, T003/T004/T018/T020/T022/T032-T035 | Full M/PC classification; unsupported excluded |
| SC-009 | US2/US5, T017/T019/T037-T039 | Q22/Q25, integrated main check later |
| SC-010 | Planning delivery; future handover discipline T042 | Structural/link/secrets/line-ending/diff/PR evidence only, no operational success claim |

All 42 T tasks map above to requirements or a directly linked foundational prerequisite. T005 defines shared records supporting FR-001-FR-019; T003/T004 are provider/platform prerequisites; T038-T042 are cross-cutting validation/documentation/delivery obligations.

## Consolidated team and Gemini input

P1 outcome/operating-model -> US1/US2/FR-001-FR-004/FR-010. Reliability/evidence -> US3/FR-005-FR-015. Gemini waiting reasons/accessibility/early acceptance mapping -> US4/FR-012/FR-017-FR-019 and Q17/Q19/Q22 (advice, not tested UX/user research). Documentation and communication/publication boundaries -> FR-020-FR-022, T038/T039 and Q23/Q25. Stages and constraints -> US5/FR-016/FR-023/FR-024.

## Planning acceptance versus execution acceptance

Planning checks establish document completeness/consistency and actual publication/check state only. Provider entitlement, scanner cloud auth, stale-controller fencing, original runtime authority export, Windows/Android canaries, PC-off delivery, cost stops, practical acceptance and integrated-main documentation currency remain later obligations. None is satisfied by checked requirements-quality items, a Draft PR or a green documentation CI run.
