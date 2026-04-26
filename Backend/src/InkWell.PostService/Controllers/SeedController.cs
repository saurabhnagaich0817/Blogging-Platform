using InkWell.PostService.Data;
using InkWell.PostService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InkWell.PostService.Controllers
{
    [ApiController]
    [Route("api/posts/seed")]
    public class SeedController : ControllerBase
    {
        private readonly PostDbContext _context;

        public SeedController(PostDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SeedPosts()
        {
            if (await _context.Posts.AnyAsync())
            {
                return BadRequest("Database already has posts. Clear it first if you want to re-seed.");
            }

            var authorId = Guid.NewGuid();
            var posts = new List<Post>();

            string[] titles = {
                "The Future of Microservices in 2026",
                "Why Angular 17 is a Game Changer",
                "Mastering .NET 8 Web APIs",
                "The Rise of Agentic AI Coding Assistants",
                "Design Systems: From Sketch to Code",
                "Clean Architecture in Modern Applications",
                "RabbitMQ vs Kafka: Which to Choose?",
                "The Art of Writing Clean JavaScript",
                "Tailwind CSS: Beyond the Basics",
                "Building Scalable Systems with YARP",
                "The Impact of AI on Software Engineering",
                "Why I Switched to Glassmorphism UI",
                "The Secret to Productive Remote Work",
                "Understanding JWT Authentication",
                "Responsive Design in the Era of Foldables",
                "Docker Tips for Faster Development",
                "The Evolution of Cloud Computing",
                "Building InkWell: A Dev Log",
                "Writing for the Web: A Guide",
                "SEO Best Practices for Bloggers"
            };

            string[] contents = {
                "Microservices continue to evolve. In 2026, we see more focus on service meshes and automated observability...",
                "Angular 17 introduced signals and a new control flow that makes it faster and more intuitive than ever...",
                ".NET 8 brings performance improvements that are staggering. From Native AOT to improved JSON serialization...",
                "AI is no longer just a chatbot; it's an agent that can write, test, and deploy code alongside you...",
                "A good design system is more than just colors and fonts. It's about consistency and scalability...",
                "Separating concerns is the heart of Clean Architecture. It makes your code testable and maintainable...",
                "RabbitMQ is great for complex routing, while Kafka shines in high-throughput event streaming...",
                "Clean code is not about following rules; it's about making your code readable for the next human...",
                "Tailwind is more than just utility classes. It's a system for building consistent interfaces quickly...",
                "YARP (Yet Another Reverse Proxy) provides a powerful toolkit for building high-performance gateways...",
                "Artificial Intelligence is changing the landscape of software engineering by automating routine tasks...",
                "Glassmorphism creates a sense of depth and hierarchy that feels premium and modern...",
                "Remote work is here to stay. The key is finding a balance between deep work and collaboration...",
                "JSON Web Tokens are the standard for secure authentication in stateless microservices...",
                "With new device forms, responsive design must adapt to multiple screens and folding hinges...",
                "Small changes in your Dockerfile can lead to massive improvements in build times...",
                "Cloud computing has moved from just hosting to providing specialized AI and serverless services...",
                "Building a platform like InkWell requires careful planning of microservices and event flows...",
                "Web content should be scannable, concise, and focused on the user's needs...",
                "SEO is not just about keywords; it's about providing value and having a fast, accessible site..."
            };

            string[] images = {
                "https://images.unsplash.com/photo-1451187580459-43490279c0fa?q=80&w=1000",
                "https://images.unsplash.com/photo-1555066931-4365d14bab8c?q=80&w=1000",
                "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?q=80&w=1000",
                "https://images.unsplash.com/photo-1677442136019-21780ecad995?q=80&w=1000",
                "https://images.unsplash.com/photo-1586717791821-3f44a563dc4c?q=80&w=1000",
                "https://images.unsplash.com/photo-1618477388954-7852f32655ec?q=80&w=1000",
                "https://images.unsplash.com/photo-1558494949-ef010cbdcc51?q=80&w=1000",
                "https://images.unsplash.com/photo-1516116216624-53e697fedbea?q=80&w=1000",
                "https://images.unsplash.com/photo-1587620962725-abab7fe55159?q=80&w=1000",
                "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?q=80&w=1000",
                "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?q=80&w=1000",
                "https://images.unsplash.com/photo-1558591710-4b4a1ae0f04d?q=80&w=1000",
                "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?q=80&w=1000",
                "https://images.unsplash.com/photo-1563986768609-322da13575f3?q=80&w=1000",
                "https://images.unsplash.com/photo-1483058712412-4245e9b90334?q=80&w=1000",
                "https://images.unsplash.com/photo-1605745341112-85968b193ef5?q=80&w=1000",
                "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?q=80&w=1000",
                "https://images.unsplash.com/photo-1499750310107-5fef28a66643?q=80&w=1000",
                "https://images.unsplash.com/photo-1455390582262-044cdead277a?q=80&w=1000",
                "https://images.unsplash.com/photo-1432888498266-38ffec3eaf0a?q=80&w=1000"
            };

            for (int i = 0; i < titles.Length; i++)
            {
                posts.Add(new Post
                {
                    PostId = Guid.NewGuid(),
                    AuthorId = authorId,
                    AuthorName = "Saurabh Nagaich",
                    Title = titles[i],
                    Slug = titles[i].ToLower().Replace(" ", "-").Replace(":", "").Replace("?", ""),
                    Content = contents[i],
                    ImageUrl = images[i],
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow.AddHours(-i * 12) // Space them out
                });
            }

            await _context.Posts.AddRangeAsync(posts);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "20 posts seeded successfully", Count = posts.Count });
        }
    }
}
