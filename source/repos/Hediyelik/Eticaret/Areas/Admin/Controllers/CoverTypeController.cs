using Hediyelik.DataAccess.Repository.IRepository;
using Hediyelik.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.View;

namespace Eticaret.Areas.Admin.Controllers;
[Area("Admin")]
public class CoverTypeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CoverTypeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<CoverType> coverTypeList = _unitOfWork.CoverType.GetAll();
        return View(coverTypeList);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CoverType obj)
    {
        if (ModelState.IsValid)
        {
            _unitOfWork.CoverType.Add(obj);
            _unitOfWork.Save();
            TempData["success"] = "Model has been added successfully";
            return RedirectToAction("Index");
        }

        return View(obj);
    }

    public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        var obj = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

        if (obj == null)
        {
            return NotFound();
        }

        return View(obj);


    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeletePost(int? id)
    {
        var obj = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

        if (obj == null)
        {
            return NotFound();
        }
        _unitOfWork.CoverType.Remove(obj);
        _unitOfWork.Save();
        TempData["success"] = "Model has been deleted successfully";

        return RedirectToAction("Index");

    }

    public IActionResult Edit(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        var coverType = _unitOfWork.CoverType.GetFirstOrDefault(u => u.Id == id);

        if (coverType == null)
        {
            return NotFound();
        }

        return View(coverType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(CoverType obj)
    {
        if (ModelState.IsValid)
        {
            _unitOfWork.CoverType.Update(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category Updated Succesfully";
            return RedirectToAction("Index"); // başka bir controller için RedirectToAction("Index","Category");
        }

        return View(obj);
    }

}
