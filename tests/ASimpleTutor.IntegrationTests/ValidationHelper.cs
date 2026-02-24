using ASimpleTutor.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ASimpleTutor.IntegrationTests;

/// <summary>
/// 验证结果类，用于存储验证过程中的结果信息
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 测试名称
    /// </summary>
    public string TestName { get; set; }
    
    /// <summary>
    /// 验证是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 验证成功时的消息
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// 验证失败时的错误信息列表
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
    
    /// <summary>
    /// 添加错误信息
    /// </summary>
    /// <param name="error">错误信息</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}

/// <summary>
/// 验证数据类，用于存储验证配置和预期结果
/// </summary>
public class ValidationData
{
    /// <summary>
    /// 验证数据的版本号
    /// </summary>
    [JsonProperty("schema_version")]
    public string SchemaVersion { get; set; }
    
    /// <summary>
    /// 验证配置
    /// </summary>
    [JsonProperty("validation")]
    public Validation Validation { get; set; }
    
    /// <summary>
    /// 验证规则
    /// </summary>
    [JsonProperty("validation_rules")]
    public ValidationRules ValidationRules { get; set; }
}

/// <summary>
/// 验证配置类，包含各种验证的配置信息
/// </summary>
public class Validation
{
    /// <summary>
    /// 文档扫描验证配置
    /// </summary>
    [JsonProperty("document_scan")]
    public DocumentScanValidation DocumentScan { get; set; }
    
    /// <summary>
    /// 知识提取验证配置
    /// </summary>
    [JsonProperty("knowledge_extraction")]
    public KnowledgeExtractionValidation KnowledgeExtraction { get; set; }
    
    /// <summary>
    /// PPT幻灯片验证配置
    /// </summary>
    [JsonProperty("ppt_slides")]
    public PptSlidesValidation PptSlides { get; set; }
    
    /// <summary>
    /// 教学提示验证配置
    /// </summary>
    [JsonProperty("teaching_hints")]
    public TeachingHintsValidation TeachingHints { get; set; }
}

/// <summary>
/// 文档扫描验证配置类
/// </summary>
public class DocumentScanValidation
{
    /// <summary>
    /// 预期的文档扫描结果
    /// </summary>
    [JsonProperty("expected")]
    public DocumentScanExpected Expected { get; set; }
    
    /// <summary>
    /// 实际的文档扫描结果文件路径
    /// </summary>
    [JsonProperty("actual_file")]
    public string ActualFile { get; set; }
}

/// <summary>
/// 文档扫描预期结果类
/// </summary>
public class DocumentScanExpected
{
    /// <summary>
    /// 文档标题
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; }
    
    /// <summary>
    /// 文档章节列表
    /// </summary>
    [JsonProperty("sections")]
    public List<ExpectedSection> Sections { get; set; }
}

/// <summary>
/// 预期章节类
/// </summary>
public class ExpectedSection
{
    /// <summary>
    /// 章节标题
    /// </summary>
    [JsonProperty("heading")]
    public string Heading { get; set; }
    
    /// <summary>
    /// 子章节列表
    /// </summary>
    [JsonProperty("subsections")]
    public List<string> Subsections { get; set; }
}

/// <summary>
/// 知识提取验证配置类
/// </summary>
public class KnowledgeExtractionValidation
{
    /// <summary>
    /// 预期的知识提取结果
    /// </summary>
    [JsonProperty("expected")]
    public KnowledgeExtractionExpected Expected { get; set; }
}

/// <summary>
/// 知识提取预期结果类
/// </summary>
public class KnowledgeExtractionExpected
{
    /// <summary>
    /// 知识点列表
    /// </summary>
    [JsonProperty("knowledge_points")]
    public List<KnowledgePoint> KnowledgePoints { get; set; }
}

/// <summary>
/// 知识点类
/// </summary>
public class KnowledgePoint
{
    /// <summary>
    /// 知识点标题
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; }
    
    /// <summary>
    /// 知识点类型
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; }
    
    /// <summary>
    /// 知识点重要性
    /// </summary>
    [JsonProperty("importance")]
    public double Importance { get; set; }
    
    /// <summary>
    /// 知识点所在章节路径
    /// </summary>
    [JsonProperty("chapter_path")]
    public List<string> ChapterPath { get; set; }
}

/// <summary>
/// PPT幻灯片验证配置类
/// </summary>
public class PptSlidesValidation
{
    /// <summary>
    /// 预期的PPT幻灯片结果
    /// </summary>
    [JsonProperty("expected")]
    public PptSlidesExpected Expected { get; set; }
}

/// <summary>
/// PPT幻灯片预期结果类
/// </summary>
public class PptSlidesExpected
{
    /// <summary>
    /// 幻灯片列表
    /// </summary>
    [JsonProperty("slides")]
    public List<Slide> Slides { get; set; }
}

/// <summary>
/// 幻灯片类
/// </summary>
public class Slide
{
    /// <summary>
    /// 幻灯片标题
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; }
    
    /// <summary>
    /// 幻灯片内容类型
    /// </summary>
    [JsonProperty("content_type")]
    public string ContentType { get; set; }
    
    /// <summary>
    /// 幻灯片包含的章节列表
    /// </summary>
    [JsonProperty("sections")]
    public List<string> Sections { get; set; }
    
    /// <summary>
    /// 幻灯片包含的子章节列表
    /// </summary>
    [JsonProperty("subsections")]
    public List<string> Subsections { get; set; }
    
    /// <summary>
    /// 幻灯片包含的关键点列表
    /// </summary>
    [JsonProperty("key_points")]
    public List<string> KeyPoints { get; set; }
}

/// <summary>
/// 教学提示验证配置类
/// </summary>
public class TeachingHintsValidation
{
    /// <summary>
    /// 预期的教学提示结果
    /// </summary>
    [JsonProperty("expected")]
    public TeachingHintsExpected Expected { get; set; }
}

/// <summary>
/// 教学提示预期结果类
/// </summary>
public class TeachingHintsExpected
{
    /// <summary>
    /// 教学提示列表
    /// </summary>
    [JsonProperty("hints")]
    public List<Hint> Hints { get; set; }
}

/// <summary>
/// 教学提示类
/// </summary>
public class Hint
{
    /// <summary>
    /// 教学提示所在章节路径
    /// </summary>
    [JsonProperty("chapter_path")]
    public List<string> ChapterPath { get; set; }
    
    /// <summary>
    /// 教学提示内容列表
    /// </summary>
    [JsonProperty("hints")]
    public List<string> Hints { get; set; }
}

/// <summary>
/// 验证规则类，用于存储各种验证的规则配置
/// </summary>
public class ValidationRules
{
    /// <summary>
    /// 文档扫描验证规则
    /// </summary>
    [JsonProperty("document_scan")]
    public DocumentScanRules DocumentScan { get; set; }
    
    /// <summary>
    /// 知识提取验证规则
    /// </summary>
    [JsonProperty("knowledge_extraction")]
    public KnowledgeExtractionRules KnowledgeExtraction { get; set; }
    
    /// <summary>
    /// PPT幻灯片验证规则
    /// </summary>
    [JsonProperty("ppt_slides")]
    public PptSlidesRules PptSlides { get; set; }
    
    /// <summary>
    /// 教学提示验证规则
    /// </summary>
    [JsonProperty("teaching_hints")]
    public TeachingHintsRules TeachingHints { get; set; }
}

/// <summary>
/// 文档扫描验证规则类
/// </summary>
public class DocumentScanRules
{
    /// <summary>
    /// 是否检查文档结构
    /// </summary>
    [JsonProperty("check_structure")]
    public bool CheckStructure { get; set; }
    
    /// <summary>
    /// 是否检查章节数量
    /// </summary>
    [JsonProperty("check_section_count")]
    public bool CheckSectionCount { get; set; }
    
    /// <summary>
    /// 是否检查内容长度
    /// </summary>
    [JsonProperty("check_content_length")]
    public bool CheckContentLength { get; set; }
}

/// <summary>
/// 知识提取验证规则类
/// </summary>
public class KnowledgeExtractionRules
{
    /// <summary>
    /// 是否检查知识点覆盖范围
    /// </summary>
    [JsonProperty("check_coverage")]
    public bool CheckCoverage { get; set; }
    
    /// <summary>
    /// 是否检查知识点重要性
    /// </summary>
    [JsonProperty("check_importance")]
    public bool CheckImportance { get; set; }
    
    /// <summary>
    /// 是否检查知识点章节路径
    /// </summary>
    [JsonProperty("check_chapter_path")]
    public bool CheckChapterPath { get; set; }
}

/// <summary>
/// PPT幻灯片验证规则类
/// </summary>
public class PptSlidesRules
{
    /// <summary>
    /// 是否检查幻灯片结构
    /// </summary>
    [JsonProperty("check_structure")]
    public bool CheckStructure { get; set; }
    
    /// <summary>
    /// 是否检查幻灯片内容类型
    /// </summary>
    [JsonProperty("check_content_type")]
    public bool CheckContentType { get; set; }
    
    /// <summary>
    /// 是否检查幻灯片关键点
    /// </summary>
    [JsonProperty("check_key_points")]
    public bool CheckKeyPoints { get; set; }
}

/// <summary>
/// 教学提示验证规则类
/// </summary>
public class TeachingHintsRules
{
    /// <summary>
    /// 是否检查教学提示章节覆盖范围
    /// </summary>
    [JsonProperty("check_chapter_coverage")]
    public bool CheckChapterCoverage { get; set; }
    
    /// <summary>
    /// 是否检查教学提示相关性
    /// </summary>
    [JsonProperty("check_hint_relevance")]
    public bool CheckHintRelevance { get; set; }
    
    /// <summary>
    /// 是否检查教学提示详细程度
    /// </summary>
    [JsonProperty("check_hint_detail")]
    public bool CheckHintDetail { get; set; }
}

/// <summary>
/// 验证辅助类，用于执行各种验证操作
/// </summary>
public class ValidationHelper
{
    /// <summary>
    /// 验证文档扫描结果
    /// </summary>
    /// <param name="outputPath">输出路径</param>
    /// <param name="validationDataPath">验证数据路径</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidateDocumentScan(string outputPath, string validationDataPath)
    {
        var result = new ValidationResult { TestName = "Document Scan Validation" };
        
        try
        {
            // 读取验证数据
            var validationData = JsonConvert.DeserializeObject<ValidationData>(File.ReadAllText(validationDataPath));
            
            // 读取实际扫描结果
            var scanResultPath = Path.Combine(outputPath, validationData.Validation.DocumentScan.ActualFile);
            var actualScanResult = JsonConvert.DeserializeObject<List<Document>>(File.ReadAllText(scanResultPath));
            
            // 验证文档数量
            if (actualScanResult.Count != 1)
            {
                result.AddError($"Expected 1 document, got {actualScanResult.Count}");
                return result;
            }
            
            var document = actualScanResult[0];
            
            // 验证文档标题
            if (document.Title != validationData.Validation.DocumentScan.Expected.Title)
            {
                result.AddError($"Expected title '{validationData.Validation.DocumentScan.Expected.Title}', got '{document.Title}'");
            }
            
            // 验证章节结构
            var expectedSections = validationData.Validation.DocumentScan.Expected.Sections;
            var actualSections = document.Sections.First().SubSections;
            
            if (actualSections.Count != expectedSections.Count)
            {
                result.AddError($"Expected {expectedSections.Count} sections, got {actualSections.Count}");
                return result;
            }
            
            for (int i = 0; i < expectedSections.Count; i++)
            {
                var expectedSection = expectedSections[i];
                var actualSection = actualSections[i];
                
                // 验证章节标题
                if (actualSection.HeadingPath.Last() != expectedSection.Heading)
                {
                    result.AddError($"Expected section {i+1} title '{expectedSection.Heading}', got '{actualSection.HeadingPath.Last()}'");
                }
                
                // 验证子章节数量
                if (actualSection.SubSections.Count != expectedSection.Subsections.Count)
                {
                    result.AddError($"Expected {expectedSection.Subsections.Count} subsections for section {i+1}, got {actualSection.SubSections.Count}");
                    continue;
                }
                
                // 验证子章节标题
                for (int j = 0; j < expectedSection.Subsections.Count; j++)
                {
                    var expectedSubsection = expectedSection.Subsections[j];
                    var actualSubsection = actualSection.SubSections[j];
                    
                    if (actualSubsection.HeadingPath.Last() != expectedSubsection)
                    {
                        result.AddError($"Expected subsection {i+1}.{j+1} title '{expectedSubsection}', got '{actualSubsection.HeadingPath.Last()}'");
                    }
                }
            }
            
            if (result.Errors.Count == 0)
            {
                result.Success = true;
                result.Message = "Document scan validation passed!";
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 验证知识提取结果
    /// </summary>
    /// <param name="validationDataPath">验证数据路径</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidateKnowledgeExtraction(string validationDataPath)
    {
        var result = new ValidationResult { TestName = "Knowledge Extraction Validation" };
        
        try
        {
            // 读取验证数据
            var validationData = JsonConvert.DeserializeObject<ValidationData>(File.ReadAllText(validationDataPath));
            
            // 验证知识点数量
            var expectedKnowledgePoints = validationData.Validation.KnowledgeExtraction.Expected.KnowledgePoints;
            if (expectedKnowledgePoints.Count == 0)
            {
                result.AddError("Expected knowledge points, but none were found in validation data");
                return result;
            }
            
            // 验证每个知识点的字段
            foreach (var knowledgePoint in expectedKnowledgePoints)
            {
                if (string.IsNullOrEmpty(knowledgePoint.Title))
                {
                    result.AddError("Knowledge point title cannot be empty");
                }
                
                if (string.IsNullOrEmpty(knowledgePoint.Type))
                {
                    result.AddError("Knowledge point type cannot be empty");
                }
                
                if (knowledgePoint.Importance < 0 || knowledgePoint.Importance > 1)
                {
                    result.AddError($"Knowledge point importance must be between 0 and 1, got {knowledgePoint.Importance}");
                }
                
                if (knowledgePoint.ChapterPath == null || knowledgePoint.ChapterPath.Count == 0)
                {
                    result.AddError("Knowledge point chapter path cannot be empty");
                }
            }
            
            if (result.Errors.Count == 0)
            {
                result.Success = true;
                result.Message = "Knowledge extraction validation passed!";
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 验证PPT幻灯片结果
    /// </summary>
    /// <param name="validationDataPath">验证数据路径</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidatePptSlides(string validationDataPath)
    {
        var result = new ValidationResult { TestName = "PPT Slides Validation" };
        
        try
        {
            // 读取验证数据
            var validationData = JsonConvert.DeserializeObject<ValidationData>(File.ReadAllText(validationDataPath));
            
            // 验证幻灯片数量
            var expectedSlides = validationData.Validation.PptSlides.Expected.Slides;
            if (expectedSlides.Count == 0)
            {
                result.AddError("Expected slides, but none were found in validation data");
                return result;
            }
            
            // 验证每张幻灯片的字段
            foreach (var slide in expectedSlides)
            {
                if (string.IsNullOrEmpty(slide.Title))
                {
                    result.AddError("Slide title cannot be empty");
                }
                
                if (string.IsNullOrEmpty(slide.ContentType))
                {
                    result.AddError("Slide content type cannot be empty");
                }
                
                // 根据内容类型验证相应的字段
                if (slide.ContentType == "overview" && (slide.Sections == null || slide.Sections.Count == 0))
                {
                    result.AddError("Overview slide must have sections");
                }
                
                if (slide.ContentType == "section" && (slide.Subsections == null || slide.Subsections.Count == 0))
                {
                    result.AddError("Section slide must have subsections");
                }
                
                if (slide.ContentType == "subsection" && (slide.KeyPoints == null || slide.KeyPoints.Count == 0))
                {
                    result.AddError("Subsection slide must have key points");
                }
            }
            
            if (result.Errors.Count == 0)
            {
                result.Success = true;
                result.Message = "PPT slides validation passed!";
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 验证教学提示结果
    /// </summary>
    /// <param name="validationDataPath">验证数据路径</param>
    /// <returns>验证结果</returns>
    public static ValidationResult ValidateTeachingHints(string validationDataPath)
    {
        var result = new ValidationResult { TestName = "Teaching Hints Validation" };
        
        try
        {
            // 读取验证数据
            var validationData = JsonConvert.DeserializeObject<ValidationData>(File.ReadAllText(validationDataPath));
            
            // 验证教学提示数量
            var expectedHints = validationData.Validation.TeachingHints.Expected.Hints;
            if (expectedHints.Count == 0)
            {
                result.AddError("Expected teaching hints, but none were found in validation data");
                return result;
            }
            
            // 验证每个教学提示的字段
            foreach (var hint in expectedHints)
            {
                if (hint.ChapterPath == null || hint.ChapterPath.Count == 0)
                {
                    result.AddError("Teaching hint chapter path cannot be empty");
                }
                
                if (hint.Hints == null || hint.Hints.Count == 0)
                {
                    result.AddError("Teaching hint content cannot be empty");
                }
            }
            
            if (result.Errors.Count == 0)
            {
                result.Success = true;
                result.Message = "Teaching hints validation passed!";
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Validation failed with exception: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 运行所有验证
    /// </summary>
    /// <param name="outputPath">输出路径</param>
    /// <param name="validationDataPath">验证数据路径</param>
    public static void RunAllValidations(string outputPath, string validationDataPath)
    {
        Console.WriteLine("\n============================================================");
        Console.WriteLine("Validation Phase - Running all validations");
        Console.WriteLine("============================================================");
        Console.WriteLine();
        
        var results = new List<ValidationResult>();
        
        // 运行文档扫描验证
        results.Add(ValidateDocumentScan(outputPath, validationDataPath));
        
        // 运行知识提取验证
        results.Add(ValidateKnowledgeExtraction(validationDataPath));
        
        // 运行PPT幻灯片验证
        results.Add(ValidatePptSlides(validationDataPath));
        
        // 运行教学提示验证
        results.Add(ValidateTeachingHints(validationDataPath));
        
        // 输出验证结果
        foreach (var validationResult in results)
        {
            Console.WriteLine($"[{validationResult.TestName}]");
            Console.WriteLine($"Status: {(validationResult.Success ? "PASSED" : "FAILED")}");
            
            if (validationResult.Success)
            {
                Console.WriteLine($"Message: {validationResult.Message}");
            }
            else
            {
                Console.WriteLine("Errors:");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            
            Console.WriteLine();
        }
        
        // 输出总体结果
        var allPassed = results.All(r => r.Success);
        Console.WriteLine("============================================================");
        Console.WriteLine($"Overall Validation Result: {(allPassed ? "ALL PASSED" : "SOME FAILED")}");
        Console.WriteLine("============================================================");
    }
}
