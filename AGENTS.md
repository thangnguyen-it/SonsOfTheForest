# SonsOfTheForest Repository Instructions

## Authority and required reading

- File này áp dụng cho toàn bộ repository, trừ khi một `AGENTS.md` ở thư mục con đưa ra hướng dẫn cụ thể hơn.
- Đọc và tuân thủ `Assets/_Docs/PROJECT_SUPREME_DIRECTIVE.md` và `Assets/_Docs/SOTF_RESEARCH_PROTOCOL.md` trước mọi milestone có liên quan đến hành vi, nội dung hoặc độ trung thực so với *Sons of the Forest*.
- Đọc kế hoạch, ghi chú nghiên cứu, quyết định kiến trúc và test hiện có liên quan trực tiếp đến phạm vi đang làm.
- Trạng thái thực tế của repository, Unity project và kết quả đo mới hơn được ưu tiên hơn tài liệu đã cũ. Khi có mâu thuẫn, ghi nhận và cập nhật nguồn sự thật phù hợp; không âm thầm bỏ qua.
- Hướng dẫn nhiệm vụ rõ ràng của người dùng có quyền ưu tiên cao hơn file này. Không tự mở rộng quyền hạn sang merge, force-push, xóa dữ liệu hoặc thay đổi ngoài phạm vi.

## Product direction

- Đây là game survival 3D production-scale, không phải game jam hoặc prototype dùng một lần.
- *Sons of the Forest* là tham chiếu về chiều sâu cơ chế và chất lượng trải nghiệm; sản phẩm trong repository phải là một triển khai độc lập.
- Không sao chép code, nội dung, nhân vật, bản đồ, binary, asset trích xuất hoặc tài sản độc quyền của game tham chiếu.
- Phát triển bằng các vertical slice nhỏ có khả năng trở thành một phần của game chính thức.
- Không cố xây toàn bộ game trong một milestone.
- Single-player là mục tiêu hiện tại. Không triển khai multiplayer nếu nhiệm vụ không yêu cầu rõ ràng.

## Primary engineering risk

Rủi ro lớn nhất là các feature hoạt động riêng lẻ nhưng không thể tích hợp với nhau.

Trước khi thêm hoặc thay đổi một hệ thống, đánh giá tác động đến:

- Gameplay
- Data và configuration
- Save/load
- UI
- Physics
- Audio và VFX
- Performance
- Testing
- Ranh giới multiplayer trong tương lai

Không bắt buộc triển khai tất cả các phần trên trong cùng milestone, nhưng phải xác định dependency, integration point, phần bị ảnh hưởng và phần được chủ ý để lại cho milestone sau.

## Working protocol

Trước mỗi nhiệm vụ:

1. Xác nhận repository root, branch, HEAD và `git status`.
2. Đọc tài liệu, code, asset và test liên quan.
3. Kiểm tra thay đổi chưa commit và bảo toàn thay đổi của người dùng.
4. Với nhiệm vụ liên quan đến Unity, dùng Unity MCP khi khả dụng để kiểm tra Editor, Console, active scene, scene dirty state, compile/update state và Play Mode.
5. Phân biệt trạng thái đã xác minh với giả định, dữ liệu lịch sử hoặc tài liệu có thể đã cũ.
6. Chọn và thực hiện một milestone có phạm vi, allow-list và tiêu chí hoàn thành rõ ràng.

Tự quyết các lựa chọn kỹ thuật thông thường có thể đảo ngược và nằm trong phạm vi nhiệm vụ. Chỉ dừng để hỏi người dùng khi cần thay đổi product intent, thêm dependency có ảnh hưởng đáng kể, thực hiện thao tác phá hủy/khó đảo ngược, xử lý tài sản hoặc giấy phép không rõ ràng, hoặc thiếu thông tin khiến các lựa chọn hợp lý dẫn đến kết quả sản phẩm khác nhau đáng kể.

## Research and fidelity

- Nghiên cứu có mục tiêu là bắt buộc trước khi triển khai một gameplay system hoặc hành vi cần tái tạo. Không nghiên cứu rộng những hệ thống ngoài phạm vi.
- Mọi bằng chứng hợp pháp liên quan đã tìm kiếm, quan sát, đo hoặc được người dùng cung cấp phải được đánh giá theo `SOTF_RESEARCH_PROTOCOL.md`.
- Phân loại rõ `VERIFIED`, `HISTORICAL`, `INFERRED`, `UNKNOWN` và `PROVISIONAL`.
- Chuyển bằng chứng phù hợp thành specification, requirement, test, configuration decision và fidelity-gap tracking. Không bỏ qua bằng chứng chỉ vì generic implementation dễ hơn.
- Khi bằng chứng xung đột, ghi lại xung đột, ưu tiên theo evidence order và chọn giải pháp an toàn, có thể thay thế.
- Giá trị chưa biết phải ở trong configuration có thể thay đổi. Không phát minh số liệu hoặc trình bày lựa chọn provisional như sự thật về game tham chiếu.
- Compile và test pass chứng minh software health, không tự động chứng minh parity.

## Architecture principles

- Ưu tiên data-driven configuration.
- Module phải có trách nhiệm, ownership và dependency rõ ràng.
- Chỉ sử dụng interface, event hoặc abstraction khi chúng giải quyết một vấn đề thực tế.
- Không áp dụng design pattern một cách máy móc.
- Gameplay logic không phụ thuộc trực tiếp vào UI hoặc presentation.
- Runtime systems không phụ thuộc trực tiếp vào cấu trúc asset tải từ bên ngoài.
- Tạo project-owned prefab, material và configuration wrapper cho asset bên ngoài.
- Persistent object cần identity ổn định khi có liên quan đến save/load.
- Không tạo singleton hoặc global manager mới nếu chưa chứng minh nhu cầu.
- Không thực hiện broad refactor ngoài phạm vi milestone hiện tại.
- Không tạo một hệ thống song song để thay thế hệ thống đang hoạt động trước khi hiểu rõ giới hạn của thiết kế hiện tại.

## Unity requirements

- Xác minh Unity version, render pipeline, package và project setting từ project thay vì chỉ dựa vào mô tả.
- Dùng Unity MCP cho scene, prefab, Console, Play Mode và validation khi khả dụng.
- Không chỉnh sửa scene hoặc prefab dạng YAML/text nếu Unity API hoặc MCP là phương pháp an toàn hơn.
- Bảo toàn GUID và file `.meta`.
- Không để Console có compile error mới sau thay đổi.
- Không tuyên bố một feature hoạt động chỉ vì code compile.
- Kiểm tra hành vi trong Play Mode hoặc standalone build khi phù hợp với feature.
- Không lưu scene bị dirty ngoài ý muốn. Sau test, xác nhận lại active scene và scene dirty state.

## Performance requirements

Mục tiêu hiện tại là gameplay ổn định ở tối thiểu 60 FPS trên cấu hình và độ phân giải mục tiêu được ghi trong baseline hiệu năng hiện hành. Dùng `Assets/_Docs/Research/R2_PERF0_PRODUCTION_PERFORMANCE_BASELINE.md` làm baseline cho đến khi có phép đo mới được phê duyệt.

Mỗi thay đổi có khả năng ảnh hưởng hiệu năng phải xem xét, theo mức độ liên quan:

- Main thread
- Render thread
- GPU frame time
- Batches và SetPass
- Triangle/vertex count
- Alpha overdraw
- Shadow cost
- Memory allocation
- Garbage collection
- Frame-time spikes
- p95 và p99, không chỉ FPS trung bình

Không tối ưu dựa trên phỏng đoán khi Unity Profiler, standalone benchmark hoặc phép đo A/B có thể cung cấp bằng chứng. Không hy sinh fidelity một cách mù quáng; xác định bottleneck trước rồi tối ưu cấp biểu diễn, khoảng cách, batching, shader, shadow, streaming hoặc simulation phù hợp.

## Asset requirements

Đối với asset bên ngoài:

- Ghi lại nguồn, publisher/tác giả, phiên bản và giấy phép hoặc điều khoản sử dụng.
- Không coi asset tải về là production-ready trước khi kiểm định.
- Chuẩn hóa scale, pivot, orientation, material, shader, texture import và naming.
- Kiểm tra LOD, billboard/impostor, collider, shadow, alpha clipping, memory và render cost.
- Không đưa tất cả variation vào runtime chỉ vì pack có sẵn.
- Không tải hoặc thêm asset mới nếu các candidate hiện có cho cùng nhu cầu chưa được kiểm tra.
- Asset cây tương tác, stump, log và phần gỗ cắt phải thống nhất về loài, vỏ cây, lõi gỗ và tỷ lệ.
- Asset của bên thứ ba phải được đặt sau project-owned wrapper; gameplay code không dựa vào hierarchy hoặc tên nội bộ dễ thay đổi của asset pack.

## Verification

Tùy theo phạm vi thay đổi, chạy các kiểm tra phù hợp:

- Compile và Console error/warning check
- Focused tests
- Complete Edit Mode suite
- Play Mode tests
- Scene/prefab smoke test
- Save/load validation
- Standalone performance benchmark
- Visual inspection từ camera và lighting production-relevant
- `git diff --check` và kiểm tra allow-list cuối

Không sửa test chỉ để che runtime defect. Nếu một bước không thể chạy, báo rõ bước nào chưa được xác minh, lý do và rủi ro còn lại.

## Git safety

- Không dùng destructive Git commands hoặc broad cleanup.
- Không xóa, reset hoặc ghi đè thay đổi chưa commit của người dùng.
- Không sửa file không liên quan chỉ để làm sạch project.
- Không commit, push, merge, rebase, amend, force-push hoặc tạo pull request nếu người dùng chưa yêu cầu hoặc nhiệm vụ không cấp quyền rõ ràng.
- Chỉ stage allow-list đã kiểm tra. Trước khi kết thúc, báo cáo toàn bộ file đã thay đổi và trạng thái staging/worktree.

## Definition of Done

Một milestone chỉ hoàn thành khi:

- Phạm vi đã thống nhất được triển khai, không chỉ được lập kế hoạch.
- Không còn compile error mới.
- Focused validation và regression validation liên quan đã chạy.
- Dependency và integration point được kiểm tra.
- Không tạo regression rõ ràng hoặc thay đổi ngoài allow-list.
- Performance budget liên quan được đo hoặc đánh giá bằng bằng chứng phù hợp.
- Tài liệu, configuration và fidelity-gap tracking cần thiết được cập nhật.
- Trạng thái scene, prefab và Git được kiểm tra lại.
- Các phần chưa xác minh, provisional, deferred hoặc còn khác game tham chiếu được báo cáo trung thực.

## Communication

Khi bắt đầu:

- Tóm tắt hiểu biết về nhiệm vụ, phạm vi và rủi ro quan trọng.
- Nêu tài liệu hoặc bằng chứng sẽ chi phối quyết định.
- Chỉ hỏi trước nếu thiếu một quyết định có ảnh hưởng lớn mà không thể suy ra an toàn.

Trong khi làm:

- Cập nhật ngắn gọn tại các mốc quan trọng hoặc khi phát hiện làm thay đổi hướng đi.
- Phân biệt kết quả đã đo/kiểm chứng với suy luận.

Khi kết thúc:

- Dẫn đầu bằng kết quả.
- Liệt kê file đã thay đổi.
- Nêu validation đã chạy và kết quả.
- Nêu rõ phần chưa kiểm chứng, provisional và gap còn lại.
- Đề xuất đúng bước hợp lý tiếp theo, nhưng không tự bắt đầu milestone khác khi chưa được giao.
