export * from './employees.service';
import { EmployeesService } from './employees.service';
export * from './hello.service';
import { HelloService } from './hello.service';
export * from './shift-assignments.service';
import { ShiftAssignmentsService } from './shift-assignments.service';
export const APIS = [EmployeesService, HelloService, ShiftAssignmentsService];
