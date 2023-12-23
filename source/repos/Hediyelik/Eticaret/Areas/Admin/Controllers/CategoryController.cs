using Hediyelik.Models;
using Hediyelik.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Hediyelik.DataAccess.Repository.IRepository;


namespace Eticaret.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoryController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Category> objCategoryList = _unitOfWork.Category.GetAll();
        return View(objCategoryList);
    }

    // GET
    public IActionResult Create()
    {
        return View();
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Category obj)
    {
        if (obj.Name == obj.DisplayOrder.ToString())
        {
            ModelState.AddModelError("CustomErorr", "The Display Order Cannot Exaclty Match The Name");
        }

        if (ModelState.IsValid)
        {
            _unitOfWork.Category.Add(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category Created Succesfully";
            return RedirectToAction("Index"); // başka bir controller için RedirectToAction("Index","Category");
        }

        return View(obj);
    }


    public IActionResult Edit(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        var category = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);
        //var category = _db.Categories.SingleOrDefault(u=>u.Id == id);
        //var category = _db.Categories.FirstOrDefault(u => u.Id == id);



        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Category obj)
    {
        if (obj.Name == obj.DisplayOrder.ToString())
        {
            ModelState.AddModelError("CustomErorr", "The Display Order Cannot Exaclty Match The Name");
        }

        if (ModelState.IsValid)
        {
            _unitOfWork.Category.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category Updated Succesfully";
            return RedirectToAction("Index"); // başka bir controller için RedirectToAction("Index","Category");
        }

        return View(obj);
    }

    public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        //var category = _db.Categories.Find(id);
        var category = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);
        //var category = _db.Categories.FirstOrDefault(u => u.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        return View(category);
    }

    // POST
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePost(int? id)
    {
        var obj = _unitOfWork.Category.GetFirstOrDefault(u => u.Id == id);

        if (obj == null)
        {
            return NotFound();
        }

        _unitOfWork.Category.Remove(obj);
        _unitOfWork.Save();
        TempData["success"] = "Category Deleted Succesfully";
        return RedirectToAction("Index"); // başka bir controller için RedirectToAction("Index","Category");            
    }


}
