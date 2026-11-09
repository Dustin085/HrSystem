import { Component, OnInit } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import {
  CreateShiftAssignmentRequest,
  EmployeeDto,
  EmployeesService,
  ShiftAssignmentDto,
  ShiftAssignmentsService
} from '../api';
import { toErrorMessages } from '../shared/api-errors';
import { shortTime, todayIso } from '../shared/dates';

@Component({
  selector: 'app-shifts-page',
  templateUrl: './shifts-page.component.html'
})
export class ShiftsPageComponent implements OnInit {
  employees: EmployeeDto[] = [];
  assignments: ShiftAssignmentDto[] = [];
  createErrors: string[] = [];
  searchErrors: string[] = [];
  saving = false;

  readonly shortTime = shortTime;

  createForm = this.fb.group({
    employeeId: ['', Validators.required],
    date: [todayIso(), Validators.required],
    startTime: ['09:00', Validators.required],
    endTime: ['18:00', Validators.required]
  });

  searchForm = this.fb.group({
    employeeId: [''],
    from: [todayIso()],
    to: ['']
  });

  constructor(
    private fb: FormBuilder,
    private employeesService: EmployeesService,
    private shiftAssignmentsService: ShiftAssignmentsService
  ) { }

  ngOnInit(): void {
    this.employeesService.listEmployees().subscribe({
      next: employees => this.employees = employees,
      error: error => this.createErrors = toErrorMessages(error)
    });

    this.search();
  }

  search(): void {
    const { employeeId, from, to } = this.searchForm.value;
    this.searchErrors = [];

    // 空字串代表「不篩選」，要轉成 undefined，產生的 service 才不會送出這個 query 參數
    this.shiftAssignmentsService
      .listShiftAssignments(employeeId || undefined, from || undefined, to || undefined)
      .subscribe({
        next: assignments => this.assignments = assignments,
        error: error => this.searchErrors = toErrorMessages(error)
      });
  }

  submit(): void {
    if (this.createForm.invalid) {
      // 把所有欄位標成 touched，讓還沒碰過的欄位也顯示錯誤（原因同 EmployeesPageComponent.submit）
      this.createForm.markAllAsTouched();
      return; // 表單不合法就不送出請求
    }

    const request: CreateShiftAssignmentRequest = this.createForm.value;

    this.saving = true;
    this.createErrors = [];

    this.shiftAssignmentsService.createShiftAssignment(request)
      .pipe(finalize(() => this.saving = false))
      .subscribe({
        next: () => this.search(),
        // 時段重疊（409）、下班早於上班、員工不存在（400）都會顯示在這裡
        error: error => this.createErrors = toErrorMessages(error)
      });
  }

  employeeLabel(employeeId: string): string {
    const employee = this.employees.find(e => e.id === employeeId);
    return employee ? `${employee.employeeNumber} ${employee.name}` : employeeId;
  }

  /** 欄位不合法「而且」使用者碰過 (touched) 才顯示錯誤，避免一打開頁面就滿版紅字。 */
  isInvalid(controlName: string): boolean {
    const control = this.createForm.get(controlName);
    return !!control && control.invalid && control.touched;
  }
}
