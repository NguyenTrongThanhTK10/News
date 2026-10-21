using News.Domain;

namespace News.Infrastructure;

// Bộ dữ liệu biên tập minh họa, không phải bản tin thực tế.
// DatabaseSeeder lưu bộ dữ liệu này qua EF Core chỉ khi bật seed minh họa.
public static class DemoArticles
{
    public static IReadOnlyList<Article> Create() => Array.AsReadOnly(new Article[]
    {
        Create(1, "Khi thành phố dành nhiều không gian hơn cho con người",
            "Từ những con đường rợp bóng cây đến quảng trường mở, một cách sống chậm và bền vững đang được hình dung giữa lòng đô thị.",
            "Một thành phố dễ sống bắt đầu từ những điều gần gũi: vỉa hè đủ rộng để đi bộ, một hàng cây che nắng và khoảng sân để hàng xóm gặp nhau. Không gian công cộng kết nối các hoạt động vốn thường bị chia cắt bởi giao thông và nhịp sống vội vàng.\n\nTrong câu chuyện đô thị xanh, trồng thêm cây chỉ là một phần. Việc kết nối công viên với trường học, nơi làm việc và các tuyến giao thông công cộng có thể giúp cư dân lựa chọn đi bộ cho những quãng đường ngắn. Thiết kế cũng cần quan tâm tới người cao tuổi, trẻ em và người dùng xe lăn.\n\nThay đổi có thể bắt đầu từ quy mô khu phố: cải tạo một khoảng đất trống, thêm ghế nghỉ dưới tán cây hoặc tổ chức một phiên chợ cuối tuần. Khi người dân cùng tham gia, không gian chung có cơ hội trở thành một phần thực sự của đời sống.",
            "doi-song", "Đời sống", "photo-1449824913935-59a10b8d2000", "Minh Anh", 6, 0),
        Create(2, "Trí tuệ nhân tạo và hành trình học cách làm việc mới",
            "Công cụ thay đổi từng ngày, nhưng khả năng đặt câu hỏi và tư duy độc lập vẫn là nền tảng.",
            "Trong một nhóm làm việc, trí tuệ nhân tạo có thể hỗ trợ tổng hợp tài liệu, phác thảo ý tưởng và xử lý các tác vụ lặp lại. Giá trị của công cụ phụ thuộc vào bài toán được giao và cách con người kiểm tra kết quả.\n\nMột quy trình thực hành hữu ích là bắt đầu với nhiệm vụ nhỏ, ghi lại đầu vào, đối chiếu đầu ra với nguồn đáng tin cậy rồi mới mở rộng. Với dữ liệu riêng tư, nhóm cần thống nhất rõ loại thông tin nào được phép đưa vào công cụ.\n\nHọc cách cộng tác với công nghệ cũng là học cách nhận biết giới hạn của nó. Phán đoán, trách nhiệm và hiểu biết về bối cảnh vẫn cần được giữ trong tay người sử dụng.",
            "cong-nghe", "Công nghệ", "photo-1485827404703-89b55fcc595e", "Hoàng Nam", 5, 1),
        Create(3, "Một Đà Lạt rất khác trong những buổi sớm mai",
            "Bỏ lại lịch trình dày đặc, thử tìm vẻ đẹp của phố núi qua một buổi đi bộ không vội.",
            "Khi phố còn yên, một con dốc quen cũng có thể mang đến trải nghiệm khác. Tiếng xe thưa, mùi cà phê mới và những khu vườn nhỏ tạo nên nhịp điệu riêng của buổi sáng vùng cao.\n\nThay vì ghé nhiều địa điểm trong cùng một ngày, du khách có thể chọn một khu vực rồi đi bộ khám phá. Một quán nhỏ, một cửa hàng sách hay câu chuyện của người bán rau có thể trở thành phần đáng nhớ nhất của chuyến đi.\n\nDu lịch chậm đi cùng sự tôn trọng nơi mình ghé thăm: giữ yên tĩnh ở khu dân cư, mang theo bình nước và hỏi trước khi chụp ảnh người khác.",
            "du-lich", "Du lịch", "photo-1476514525535-07fb3b4ae5f1", "Thảo Nguyên", 4, 2),
        Create(4, "Những tiệm sách nhỏ giữ nhịp văn hóa trong phố",
            "Không chỉ bán sách, những không gian độc lập còn là nơi gặp gỡ của các cộng đồng yêu tri thức.",
            "Giữa những dãy phố đông đúc, tiệm sách nhỏ mang lại một khoảng dừng. Người đọc ghé qua để tìm một cuốn sách, rồi có thể ở lại vì một cuộc trò chuyện.\n\nCác buổi đọc chung, trao đổi sách cũ và gặp gỡ tác giả giúp kết nối những người có cùng mối quan tâm. Quy mô nhỏ cho phép mỗi tiệm tự xây dựng cá tính qua cách chọn sách và cách đón khách.\n\nĐể những không gian này tiếp tục hoạt động, sự ủng hộ đều đặn của độc giả quan trọng hơn một lần ghé thăm theo trào lưu. Mua một cuốn sách hoặc giới thiệu đúng người cũng là cách góp phần giữ nhịp văn hóa địa phương.",
            "van-hoa", "Văn hóa", "photo-1507842217343-583bb7270b66", "Hà Phương", 4, 3),
        Create(5, "Lớp học mở: khi sự tò mò dẫn lối việc học",
            "Những dự án gần gũi giúp học sinh kết nối kiến thức trong sách với thế giới bên ngoài.",
            "Một bài học có thể bắt đầu từ câu hỏi rất đơn giản: vì sao sân trường nóng hơn khu vườn bên cạnh? Từ đó, học sinh đo nhiệt độ, ghi chép, đọc tài liệu và cùng đề xuất giải pháp.\n\nCách học theo dự án tạo cơ hội thực hành nhiều kỹ năng cùng lúc. Giáo viên đóng vai trò thiết kế tình huống và hỗ trợ học sinh kiểm tra giả thuyết, trong khi mục tiêu kiến thức vẫn cần được xác định rõ.\n\nĐánh giá quá trình nên ghi nhận cách đặt câu hỏi, chất lượng bằng chứng và khả năng hợp tác. Một sản phẩm chưa hoàn hảo vẫn có thể phản ánh hành trình học tập nhiều giá trị.",
            "giao-duc", "Giáo dục", "photo-1503676260728-1c00da094a0b", "Thu Hà", 5, 4),
        Create(6, "Chạy bộ để tìm lại khoảng thời gian cho chính mình",
            "Một đôi giày và một cung đường quen có thể mở ra thói quen sống chủ động hơn.",
            "Với nhiều người, chạy bộ không bắt đầu bằng mục tiêu thành tích. Đó đơn giản là một khoảng thời gian tách khỏi màn hình và lắng nghe nhịp sống quanh mình.\n\nNhững nhóm chạy cộng đồng tạo thêm động lực nhờ lịch hẹn đều đặn và sự đồng hành. Mỗi người có thể lựa chọn nhịp độ phù hợp, xen kẽ đi bộ và dành thời gian nghỉ.\n\nCâu chuyện của một thói quen bền vững thường nằm ở sự đều đặn. Một cung đường ngắn gần nhà có thể hữu ích hơn kế hoạch nhiều tham vọng nhưng khó duy trì.",
            "the-thao", "Thể thao", "photo-1461896836934-ffe607ba8211", "Đức Minh", 3, 5),
        Create(7, "Bữa cơm nhà và những kết nối giản dị",
            "Giữa lịch trình bận rộn, căn bếp vẫn là nơi những câu chuyện gia đình được tiếp nối.",
            "Một bữa cơm không cần cầu kỳ để trở thành khoảng thời gian đáng nhớ. Việc cùng chọn món, sơ chế và dọn bàn cho mỗi người một cơ hội tham gia.\n\nCăn bếp cũng lưu giữ ký ức qua cách nêm nếm và những công thức truyền miệng. Ghi lại món ăn quen của gia đình có thể là cách lưu giữ một phần câu chuyện của thế hệ trước.\n\nBắt đầu bằng một buổi tối cố định mỗi tuần, cả nhà có thể cùng tạo ra một nghi thức nhỏ: để điện thoại sang bên và dành trọn thời gian cho nhau.",
            "doi-song", "Đời sống", "photo-1555939594-58d7cb561ad1", "Minh Anh", 4, 6),
        Create(8, "Thiết kế số bắt đầu từ việc lắng nghe người dùng",
            "Một sản phẩm dễ sử dụng thường đến từ những quyết định nhỏ được kiểm chứng cẩn thận.",
            "Giao diện đẹp thu hút sự chú ý, nhưng trải nghiệm thuận tiện giúp người dùng hoàn thành công việc. Nhà thiết kế cần hiểu bối cảnh sử dụng trước khi lựa chọn màu sắc và bố cục.\n\nQuan sát người dùng thử một tác vụ cụ thể có thể phát hiện những điểm vướng mà nhóm phát triển không nhận ra. Một nhãn nút rõ nghĩa, độ tương phản tốt và trạng thái lỗi có hướng xử lý đều tạo khác biệt.\n\nKhả năng tiếp cận nên được cân nhắc từ đầu: điều hướng bằng bàn phím, văn bản thay thế cho hình ảnh và cách bố trí phù hợp với màn hình nhỏ.",
            "cong-nghe", "Công nghệ", "photo-1498050108023-c5249f4df085", "Hoàng Nam", 6, 7),
        Create(9, "Về miền biển, nghe làng chài kể chuyện",
            "Một chuyến đi gần hơn với đời sống địa phương qua phiên chợ sớm và những con thuyền trở về.",
            "Buổi sáng ở làng chài bắt đầu khi nhiều du khách còn ngủ. Bến thuyền đón những mẻ cá, chợ nhỏ mở hàng và các gia đình chuẩn bị cho một ngày mới.\n\nỞ lại lâu hơn tại một điểm đến giúp người đi hiểu thêm nhịp sống địa phương. Chọn dịch vụ do cư dân cung cấp cũng tạo cơ hội để lợi ích của du lịch đến gần cộng đồng.\n\nĐằng sau bức ảnh đẹp là không gian làm việc và sinh hoạt của người khác. Giữ khoảng cách phù hợp, hỏi trước khi chụp và mang rác về là những cách ứng xử giản dị.",
            "du-lich", "Du lịch", "photo-1507525428034-b723cf961d3e", "Thảo Nguyên", 5, 8),
        Create(10, "Âm nhạc sống và sức hút của những sân khấu nhỏ",
            "Khoảng cách gần giữa nghệ sĩ và khán giả tạo nên một trải nghiệm giàu cảm xúc.",
            "Một sân khấu nhỏ không có nhiều hiệu ứng vẫn có thể để lại dấu ấn. Khán giả nghe rõ câu chuyện trước mỗi bài hát và nhìn thấy cách các nhạc công tương tác.\n\nNhững không gian biểu diễn độc lập tạo cơ hội thử nghiệm cho nghệ sĩ mới. Chương trình có thể kết hợp sáng tác riêng với những bản phối khác lạ để tìm tiếng nói phù hợp.\n\nVới người nghe, sự hiện diện trọn vẹn là một phần của trải nghiệm. Tạm cất điện thoại có thể giúp ta chú ý hơn đến những chi tiết chỉ xuất hiện trong khoảnh khắc trực tiếp.",
            "van-hoa", "Văn hóa", "photo-1516280440614-37939bbacd81", "Hà Phương", 4, 9),
        Create(11, "Học một kỹ năng mới bằng những bước nhỏ mỗi ngày",
            "Thói quen tự học bền vững có thể bắt đầu từ mục tiêu vừa sức và một cuốn sổ ghi chép.",
            "Một kỹ năng mới thường khiến người học hào hứng lúc đầu, rồi nhanh chóng bối rối trước lượng tài liệu quá lớn. Chọn một mục tiêu nhỏ và rõ ràng giúp thu hẹp điểm bắt đầu.\n\nThực hành nên đi cùng phản hồi. Sau mỗi buổi học, ghi lại điều đã hiểu, điều còn vướng và việc sẽ thử ở buổi tiếp theo. Nhờ đó, quá trình tiến bộ có thể quan sát được.\n\nMột cộng đồng học tập phù hợp cũng tạo thêm động lực. Chia sẻ sản phẩm nhỏ và nhận góp ý cụ thể thường hữu ích hơn chỉ theo dõi số giờ đã học.",
            "giao-duc", "Giáo dục", "photo-1434030216411-0b793f4b4173", "Thu Hà", 4, 10),
        Create(12, "Cuối tuần trên những cung đường đạp xe",
            "Khám phá một góc quen của thành phố từ góc nhìn khác, chậm rãi và nhiều kết nối hơn.",
            "Đạp xe cho phép người đi cảm nhận khoảng cách theo cách khác. Một quán quen, hàng cây hoặc đoạn đường ven sông có thể trở thành điểm dừng thú vị.\n\nTrước mỗi chuyến đi, nhóm nên thống nhất cung đường, thời gian nghỉ và cách giữ liên lạc. Kiểm tra xe, chuẩn bị nước và chọn trang bị phù hợp giúp hành trình thuận tiện hơn.\n\nNhững chuyến đi gần nhà không nhất thiết cần kế hoạch phức tạp. Điều tạo nên trải nghiệm thường là sự quan sát và thời gian dành cho những người đồng hành.",
            "the-thao", "Thể thao", "photo-1541625602330-2277b4c46182", "Đức Minh", 3, 11)
    });


    private static Article Create(int id, string title, string summary, string content,
        string slug, string category, string image, string author, int minutes, int hoursAgo) =>
        new(id, title, summary, content, slug, category,
            $"https://images.unsplash.com/{image}?auto=format&fit=crop&w=1200&q=85",
            new DateTimeOffset(2026, 10, 21, 9, 0, 0, TimeSpan.FromHours(7)).AddHours(-hoursAgo),
            author, minutes);
}
