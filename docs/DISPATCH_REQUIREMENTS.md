# Dispatch reference coverage — 17 October 2026

The supplied Dispatch PDF was fully text-reviewed: 25 physical pages including cover, with printed pages 1–24. Page references below use printed page numbers. Twenty-three selected DAL/service/model files were downloaded; primary DAL public-method indexes and selected implementations were reviewed. This is behavioral reference work, not a full service-layer or vendor-parity certification. Reference uploads and vendor code are not committed. Green Ink code and interface are original.

| ID | Reference / requirement | Green Ink coverage | Remaining scope |
|---|---|---|---|
| DS-01 | pp3–5 areas/shipping points | Scoped setup, packing/loading bins, policy snapshots | Fine-grained area grants |
| DS-02 | pp3–5 single/group picking | Single/group mode, grouped scan submission | Optimized travel/bin route ordering |
| DS-03 | pp3–5 allow extra picking | Explicit area policy; excess must be returned before completion | Customer-specific excess delivery tolerances |
| DS-04 | pp3–5 product/collective packing | Area policy plus snapshotted customer product rule | Allocation split across HUs requires separate picks |
| DS-05 | pp3–5 SAP/internal HUs and PGI area | Local HUs, controlled loading and local issue | SAP HU/TOR/TOC and packing-area direct PGI |
| DS-06 | pp5–6 customer instructions | Versioned picking/packing/shipping/safety text and shelf-life rules | Excel upload/download templates |
| DS-07 | pp6–7 packages | Dimensions, tare/max gross, immutable package snapshots | Physical packaging inventory consumption |
| DS-08 | pp6–7 HU labels | Generated local HU QR/browser preview | Preprinted HU allocation, GS1 SSCC and certified printing |
| DS-09 | pp6–7 temperature devices | Required serial capture and active serial uniqueness | Telemetry/device provisioning |
| DS-10 | p7 external warehouses | Receiving plant/storage/gate reference master | STO open-days horizon and remote synchronization |
| DS-11 | pp8–10 calendar | Scheduled shipment list, priority and assignment | Full calendar grid, shift capacity and recurring plans |
| DS-12 | pp8–10 grouped deliveries | One or more existing delivery orders, including different customers | Multi-line SAP DN ingestion; existing DNs remain single material |
| DS-13 | pp8–10 route/carrier | Route reference plus carrier/truck entry | Carrier API and route master optimization |
| DS-14 | pp8–10 picker/reassignment | Named scoped operator, manager assignment/rescheduling with reason | Picker groups and configurable self/everyone permission matrix |
| DS-15 | pp8–10 shipment progress | State badges, per-line reconciliation, event history | Vendor-specific status icon equivalence |
| DS-16 | pp10–14 instructions/PPE | Required instruction/safety acknowledgments | PPE equipment certification integration |
| DS-17 | pp10–14 scans/quantity | Exact pallet and source-bin confirmation, positive quantities | RFID reader adapter |
| DS-18 | pp10–14 stock selection | Released finished stock, free quantity, expiry, customer shelf life, FEFO ordering | Dynamic ERP allocation/resynchronization |
| DS-19 | pp10–14 bin-to-bin | Dedicated picked pallet moves to packing then loading bins | Arbitrary dispatch bin route editor |
| DS-20 | pp10–14 live locating | Current database bin and availability | RFID distance/location stream |
| DS-21 | pp10–14 completion | Every line reconciles exactly; extra picks returned | External delivery tolerance approval |
| DS-22 | pp14–19 pending pack | Separate pick/pack workspaces with allocation readiness | Multi-level container hierarchy |
| DS-23 | pp14–19 product/batch checks | Linked actual batch, exact picked-label validation, quality recheck | Customer batch-specific external acceptance |
| DS-24 | pp14–19 open/close HU | Create, pack allocations, verify gross, close | Automated scales; manual non-kg unit weight conversion |
| DS-25 | pp14–19 packing instructions | Snapshotted instructions and explicit confirmation | Image/document attachments |
| DS-26 | pp14–19 PGI | Truck/carrier, scan loading, all-loaded gate, manager local issue | SAP acknowledgment and commercial shipping documents |
| DS-27 | pp20–24 shuttle setup | Destination warehouse and material lines, same pick/pack/load sequence | SAP STO/DO linking |
| DS-28 | pp20–24 shuttle issue | Local transfer-out ledger and LOCAL-PGI reference | Remote receipt and SAP transaction posting |
| DS-29 | pp20–24 transfer logs | Scoped immutable event/stock history and displayed-event CSV | External SAP transaction listing |
| DS-30 | supplied RevertPickListDAL | Unload, unpack, return picked stock, cancel | Partial post-issue return |
| DS-31 | supplied PostGoodsIssueDAL | Atomic local issue, whole-run physical-return reversal, preserved shipments | External ERP reversal/reconciliation |
| DS-32 | all mutations | Plant scope, assignment, manager gates, signed command replay, serializable stock posting | Production load/security/UAT certification |

## Selected logic anchors

- DispatchServiceDAL: `GetShelfPeriodSetting` (line869), `CheckPickingCompletedForDeliveryOrder` (line1653), instruction/area and pending-quantity methods informed shelf-life, acknowledgment and completion gates.
- RevertPickListDAL: `RevertPickedMaterial` (line267) informed return behavior. Green Ink retains append-only movements and original shipments rather than reproducing destructive legacy cleanup.
- PostGoodsIssueDAL: `GetDeliveryOrderStatusAndAnyFailure` (line267) informed explicit state/failure checks. Local success never represents an SAP success response.
- DispatchConfigDAL, CustomerServiceDAL, PackagingDetailsDAL, DailyCalendarDAL, PickerServiceDAL, PickListViewDAL and ReAssignDAL method indexes informed configuration, planning, scan/packing and assignment boundaries. RoutingDAL/TrackIBCDAL and selected service/model files were inspected selectively; unreviewed methods are not claimed implemented.

## Accounting choices requiring plant UAT

Each pick creates a dedicated reserved pallet. An allocation is packed into one HU; make separate picks to divide stock across HUs. Collective HUs may contain different materials/UOMs, but totals are never added across units. Gross-weight checks include known kg contents plus tare; piece weights are not invented. Unknown expiry is disallowed when customer minimum shelf life is positive. Overpick is allowed only by policy and cannot become an excess shipment. Reversal requires manager confirmation of physical return, restores stock on quality hold, and keeps original issue/shipment history. Local shuttle issue reduces local inventory without inventing destination receipt.
